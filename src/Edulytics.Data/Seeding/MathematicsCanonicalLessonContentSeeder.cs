using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Imports reviewed Edulytics canonical lesson bodies.
///
/// It never creates official curriculum identities or alignments.
/// Pedagogical LessonCode and OutcomeCodes must already exist and must match
/// the accepted official curriculum graph exactly.
/// </summary>
public sealed class MathematicsCanonicalLessonContentSeeder
{
    private const long AdvisoryLockKey = 27500029;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private readonly EdulyticsDbContext _db;

    public MathematicsCanonicalLessonContentSeeder(
        EdulyticsDbContext db) =>
        _db = db;

    public async Task SeedAsync(
        CancellationToken ct = default)
    {
        var documents = LoadEmbeddedDocuments();

        // No reviewed runtime pack is safer than fabricated content.
        // Phase 29 remains open until approved packs are supplied.
        if (documents.Count == 0)
            return;

        await SeedDocumentsAsync(documents, ct);
    }

    public static IReadOnlyList<CanonicalLessonContentPackDocument>
        LoadEmbeddedDocuments()
    {
        var assembly =
            typeof(MathematicsCurriculumPackRegistry).Assembly;

        var names =
            assembly.GetManifestResourceNames()
                .Where(
                    x => x.EndsWith(
                        ".lesson-content-pack.json",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

        var result =
            new List<CanonicalLessonContentPackDocument>();

        foreach (var name in names)
        {
            using var stream =
                assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException(
                    $"Cannot open canonical lesson content resource {name}.");

            var document =
                JsonSerializer.Deserialize<
                    CanonicalLessonContentPackDocument>(
                    stream,
                    JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Invalid canonical lesson content resource {name}.");

            CanonicalLessonContentPackContract.Validate(document);
            result.Add(document);
        }

        return result;
    }

    public async Task SeedDocumentsAsync(
        IReadOnlyCollection<CanonicalLessonContentPackDocument> documents,
        CancellationToken ct = default)
    {
        if (documents.Count == 0)
            return;

        foreach (var document in documents)
            CanonicalLessonContentPackContract.Validate(document);

        ValidateDistinctTargets(documents);

        if (_db.Database.IsNpgsql())
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(ct);

            await _db.Database.ExecuteSqlRawAsync(
                $"SELECT pg_advisory_xact_lock({AdvisoryLockKey});",
                ct);

            foreach (var document in documents
                         .OrderBy(x => x.PackCode, StringComparer.Ordinal)
                         .ThenBy(x => x.VersionCode, StringComparer.Ordinal))
            {
                await SeedOneAsync(document, ct);
            }

            await transaction.CommitAsync(ct);
            return;
        }

        foreach (var document in documents
                     .OrderBy(x => x.PackCode, StringComparer.Ordinal)
                     .ThenBy(x => x.VersionCode, StringComparer.Ordinal))
        {
            await SeedOneAsync(document, ct);
        }
    }

    private async Task SeedOneAsync(
        CanonicalLessonContentPackDocument document,
        CancellationToken ct)
    {
        var state =
            await _db.CurriculumPackImportStates
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.FrameworkCode == document.PackCode &&
                        x.VersionCode == document.VersionCode &&
                        x.IsComplete,
                    ct)
            ?? throw new InvalidOperationException(
                $"Accepted curriculum pack not found for canonical content: " +
                $"{document.PackCode}/{document.VersionCode}.");

        var requestedCodes =
            document.Lessons
                .Select(x => x.LessonCode)
                .ToArray();

        var persistedLessons =
            await _db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        requestedCodes.Contains(x.Code))
                .ToArrayAsync(ct);

        var lessonByCode =
            persistedLessons.ToDictionary(
                x => x.Code,
                StringComparer.Ordinal);

        if (lessonByCode.Count != requestedCodes.Length)
        {
            var missing =
                requestedCodes
                    .Where(x => !lessonByCode.ContainsKey(x))
                    .OrderBy(x => x, StringComparer.Ordinal);

            throw new InvalidOperationException(
                $"Canonical content references unknown pedagogical lesson(s) " +
                $"in {document.PackCode}: {string.Join(", ", missing)}.");
        }

        foreach (var sourceLesson in document.Lessons)
        {
            var lesson = lessonByCode[sourceLesson.LessonCode];

            var actualOutcomeCodes =
                await (
                    from mapping in
                        _db.CurriculumPedagogicalLessonOutcomes
                            .AsNoTracking()
                    join node in
                        _db.CurriculumPackContentNodes
                            .AsNoTracking()
                        on mapping.OutcomeNodeId equals node.Id
                    where
                        mapping.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        mapping.PedagogicalLessonId == lesson.Id
                    orderby mapping.SortOrder
                    select node.Code)
                .ToArrayAsync(ct);

            var expectedOutcomes =
                sourceLesson.OutcomeCodes.ToHashSet(
                    StringComparer.Ordinal);

            var actualOutcomes =
                actualOutcomeCodes.ToHashSet(
                    StringComparer.Ordinal);

            if (!expectedOutcomes.SetEquals(actualOutcomes))
            {
                throw new InvalidOperationException(
                    $"Canonical content OutcomeCode drift for " +
                    $"{document.PackCode}/{sourceLesson.LessonCode}. " +
                    $"Expected [{string.Join(", ", expectedOutcomes.OrderBy(x => x))}], " +
                    $"actual [{string.Join(", ", actualOutcomes.OrderBy(x => x))}].");
            }

            await UpsertLessonContentAsync(
                state.FrameworkVersionId,
                lesson,
                document,
                sourceLesson,
                ct);
        }
    }

    private async Task UpsertLessonContentAsync(
        Guid frameworkVersionId,
        CurriculumPedagogicalLesson lesson,
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson sourceLesson,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var content =
            await _db.CurriculumLessonContents
                .SingleOrDefaultAsync(
                    x => x.PedagogicalLessonId == lesson.Id,
                    ct);

        if (content is null)
        {
            content =
                new CurriculumLessonContent
                {
                    Id = G(
                        $"canonical-content|{frameworkVersionId}|{lesson.Id}"),
                    FrameworkVersionId = frameworkVersionId,
                    PedagogicalLessonId = lesson.Id,
                    Status = document.Status,
                    ContentVersion = document.ContentVersion,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    RowVersion = []
                };

            ApplyStatusMetadata(content, document.Status, now);

            _db.CurriculumLessonContents.Add(content);
        }
        else
        {
            if (content.FrameworkVersionId != frameworkVersionId ||
                content.PedagogicalLessonId != lesson.Id)
            {
                throw new InvalidOperationException(
                    $"Canonical lesson identity drift: {sourceLesson.LessonCode}.");
            }

            if (!string.Equals(
                    content.ContentVersion,
                    document.ContentVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Refusing silent canonical content-version replacement for " +
                    $"{sourceLesson.LessonCode}. Existing={content.ContentVersion}, " +
                    $"incoming={document.ContentVersion}.");
            }

            if ((int)content.Status > (int)document.Status)
            {
                throw new InvalidOperationException(
                    $"Refusing canonical content status downgrade for " +
                    $"{sourceLesson.LessonCode}: " +
                    $"{content.Status} -> {document.Status}.");
            }

            if (content.Status != document.Status)
            {
                content.Status = document.Status;
                content.UpdatedAtUtc = now;
            }

            ApplyStatusMetadata(content, document.Status, now);
        }

        var existingTranslations =
            await _db.CurriculumLessonContentTranslations
                .Where(
                    x =>
                        x.CurriculumLessonContentId ==
                            content.Id)
                .ToArrayAsync(ct);

        var incomingCultures =
            sourceLesson.Translations
                .Select(x => x.CultureCode)
                .ToHashSet(StringComparer.Ordinal);

        var unexpected =
            existingTranslations
                .Where(
                    x => !incomingCultures.Contains(x.CultureCode))
                .Select(x => x.CultureCode)
                .ToArray();

        if (unexpected.Length != 0)
        {
            throw new InvalidOperationException(
                $"Canonical translation drift for {sourceLesson.LessonCode}. " +
                $"Existing unexpected culture(s): {string.Join(", ", unexpected)}.");
        }

        var existingByCulture =
            existingTranslations.ToDictionary(
                x => x.CultureCode,
                StringComparer.Ordinal);

        foreach (var incoming in sourceLesson.Translations)
        {
            if (existingByCulture.TryGetValue(
                    incoming.CultureCode,
                    out var current))
            {
                EnsureTranslationMatches(
                    sourceLesson.LessonCode,
                    current,
                    incoming);

                continue;
            }

            _db.CurriculumLessonContentTranslations.Add(
                new CurriculumLessonContentTranslation
                {
                    Id = G(
                        $"canonical-translation|{content.Id}|{incoming.CultureCode}"),
                    CurriculumLessonContentId = content.Id,
                    CultureCode = incoming.CultureCode,
                    Title = incoming.Title,
                    Explanation = incoming.Explanation,
                    KeyConceptsAndRules =
                        incoming.KeyConceptsAndRules,
                    WorkedExamples =
                        incoming.WorkedExamples,
                    StepByStepSolutions =
                        incoming.StepByStepSolutions,
                    CommonMistakes =
                        incoming.CommonMistakes,
                    QuickSummary =
                        incoming.QuickSummary,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    RowVersion = []
                });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ApplyStatusMetadata(
        CurriculumLessonContent content,
        CanonicalLessonContentStatus status,
        DateTime now)
    {
        if (status is
            CanonicalLessonContentStatus.Verified or
            CanonicalLessonContentStatus.Published)
        {
            content.VerifiedAtUtc ??= now;
        }

        if (status ==
            CanonicalLessonContentStatus.Published)
        {
            content.PublishedAtUtc ??= now;
        }
    }

    private static void EnsureTranslationMatches(
        string lessonCode,
        CurriculumLessonContentTranslation current,
        CanonicalLessonContentPackTranslation incoming)
    {
        if (!string.Equals(
                current.Title,
                incoming.Title,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.Explanation,
                incoming.Explanation,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.KeyConceptsAndRules,
                incoming.KeyConceptsAndRules,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.WorkedExamples,
                incoming.WorkedExamples,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.StepByStepSolutions,
                incoming.StepByStepSolutions,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.CommonMistakes,
                incoming.CommonMistakes,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.QuickSummary,
                incoming.QuickSummary,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Canonical lesson body drift for " +
                $"{lessonCode}:{incoming.CultureCode}. " +
                "Refusing silent rewrite of reviewed content.");
        }
    }

    private static void ValidateDistinctTargets(
        IEnumerable<CanonicalLessonContentPackDocument> documents)
    {
        var keys =
            documents
                .SelectMany(
                    document =>
                        document.Lessons.Select(
                            lesson =>
                                $"{document.PackCode}\u001f" +
                                $"{document.VersionCode}\u001f" +
                                lesson.LessonCode))
                .ToArray();

        if (keys.Length !=
            keys.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidOperationException(
                "A pedagogical lesson appears in more than one canonical content pack document.");
        }
    }

    private static Guid G(string value)
    {
        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

        Span<byte> bytes =
            stackalloc byte[16];

        hash.AsSpan(0, 16)
            .CopyTo(bytes);

        bytes[6] =
            (byte)((bytes[6] & 0x0f) | 0x50);

        bytes[8] =
            (byte)((bytes[8] & 0x3f) | 0x80);

        return new Guid(bytes);
    }
}
