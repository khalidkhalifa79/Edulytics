using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Seeds platform-scoped Edulytics pedagogical lessons without mutating verified
/// official curriculum packs.
///
/// UAE keeps its verified official Lesson identities and verified
/// LessonStandardAlignment links.
///
/// England, Common Core and Poland do not publish verified Lesson nodes in the
/// accepted pack snapshots. For those packs Edulytics creates a grade/pathway-
/// specific pedagogical lesson for each applicable official Standard/Outcome.
/// Each created lesson is mapped directly to that exact official node. This is a
/// structural, deterministic alignment: no fuzzy text matching and no invented
/// official relationship.
/// </summary>
public sealed class MathematicsPedagogicalLessonSeeder
{
    private static readonly string[] SupportedCodes =
    [
        MathematicsCurriculumPackRegistry.EnglandCode,
        MathematicsCurriculumPackRegistry.CommonCoreCode,
        MathematicsCurriculumPackRegistry.UaeCode,
        MathematicsCurriculumPackRegistry.PolandCode
    ];

    private readonly EdulyticsDbContext _db;

    public MathematicsPedagogicalLessonSeeder(EdulyticsDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        MathematicsCurriculumPackRegistry.Validate();

        var states = await _db.CurriculumPackImportStates
            .AsNoTracking()
            .Where(x =>
                x.IsComplete &&
                SupportedCodes.Contains(x.FrameworkCode))
            .ToArrayAsync(ct);

        if (states.Length != 4 ||
            !SupportedCodes.ToHashSet(StringComparer.Ordinal)
                .SetEquals(states.Select(x => x.FrameworkCode)))
        {
            throw new InvalidOperationException(
                "Exactly four accepted Mathematics pack import states are required before pedagogical lesson seeding.");
        }

        var stateByCode = states.ToDictionary(
            x => x.FrameworkCode,
            StringComparer.Ordinal);

        var expectedLessons = new List<CurriculumPedagogicalLesson>();
        var expectedMappings = new List<CurriculumPedagogicalLessonOutcome>();

        await BuildUaeAsync(
            stateByCode[MathematicsCurriculumPackRegistry.UaeCode],
            expectedLessons,
            expectedMappings,
            ct);

        await BuildOutcomeBackedLessonsAsync(
            stateByCode,
            expectedLessons,
            expectedMappings,
            ct);

        ValidateExpectedGraph(expectedLessons, expectedMappings);

        await UpsertLessonsAsync(expectedLessons, ct);
        await UpsertMappingsAsync(expectedMappings, ct);
        await RemoveStaleLessonsSafelyAsync(expectedLessons, ct);
    }

    private async Task BuildUaeAsync(
        CurriculumPackImportState state,
        ICollection<CurriculumPedagogicalLesson> expectedLessons,
        ICollection<CurriculumPedagogicalLessonOutcome> expectedMappings,
        CancellationToken ct)
    {
        var nodes = await _db.CurriculumPackContentNodes
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == state.FrameworkVersionId &&
                x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode &&
                x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToArrayAsync(ct);

        var lessons = nodes
            .Where(x => x.NodeKind == "Lesson")
            .ToArray();

        if (lessons.Length != 42)
        {
            throw new InvalidOperationException(
                "UAE pedagogical seed requires exactly 42 verified official Lesson nodes.");
        }

        var nodeById = nodes.ToDictionary(x => x.Id);
        var now = DateTime.UtcNow;

        foreach (var lesson in lessons)
        {
            var unit = lesson.ParentId.HasValue &&
                       nodeById.TryGetValue(lesson.ParentId.Value, out var parent)
                ? parent
                : null;

            expectedLessons.Add(new CurriculumPedagogicalLesson
            {
                // Reuse the verified official Lesson id so existing canonical
                // Phase 29 UAE content stays attached without data migration.
                Id = lesson.Id,
                FrameworkVersionId = lesson.FrameworkVersionId,
                OfficialLessonNodeId = lesson.Id,
                Code = $"PED:{lesson.Code}",
                UnitKey = unit?.Code ?? string.Empty,
                UnitTitle = unit?.Title ?? string.Empty,
                Title = lesson.Title,
                LogicalLevelFrom = lesson.LogicalLevelFrom,
                LogicalLevelTo = lesson.LogicalLevelTo,
                NativeLevel = lesson.NativeLevel,
                Pathway = lesson.Pathway,
                SortOrder = lesson.SortOrder,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        var lessonIds = lessons.Select(x => x.Id).ToArray();
        var links = await _db.CurriculumPackNodeLinks
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == state.FrameworkVersionId &&
                lessonIds.Contains(x.FromNodeId) &&
                x.LinkKind == "LessonStandardAlignment")
            .OrderBy(x => x.SortOrder)
            .ToArrayAsync(ct);

        if (links.Length != 48)
        {
            throw new InvalidOperationException(
                "UAE pedagogical seed requires exactly 48 verified LessonStandardAlignment links.");
        }

        foreach (var link in links)
        {
            expectedMappings.Add(new CurriculumPedagogicalLessonOutcome
            {
                PedagogicalLessonId = link.FromNodeId,
                FrameworkVersionId = link.FrameworkVersionId,
                OutcomeNodeId = link.ToNodeId,
                SortOrder = link.SortOrder
            });
        }
    }

    private async Task BuildOutcomeBackedLessonsAsync(
        IReadOnlyDictionary<string, CurriculumPackImportState> stateByCode,
        ICollection<CurriculumPedagogicalLesson> expectedLessons,
        ICollection<CurriculumPedagogicalLessonOutcome> expectedMappings,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        foreach (var pack in MathematicsCurriculumPackRegistry.All
                     .Where(x => x.Code != MathematicsCurriculumPackRegistry.UaeCode))
        {
            var state = stateByCode[pack.Code];

            var nodes = await _db.CurriculumPackContentNodes
                .AsNoTracking()
                .Where(x =>
                    x.FrameworkVersionId == state.FrameworkVersionId &&
                    x.FrameworkCode == pack.Code &&
                    x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToArrayAsync(ct);

            var officialOutcomes = nodes
                .Where(x =>
                    x.IsOfficial &&
                    (x.NodeKind == "Standard" || x.NodeKind == "Outcome"))
                .ToArray();

            if (officialOutcomes.Length != state.OfficialNodeCount)
            {
                throw new InvalidOperationException(
                    $"Official outcome count drift for {pack.Code}. Expected {state.OfficialNodeCount}, got {officialOutcomes.Length}.");
            }

            var nodeById = nodes.ToDictionary(x => x.Id);
            var levels = pack.Levels
                .GroupBy(x => new { x.LogicalLevel, x.NativeLabel, x.Pathway })
                .Select(x => x.First())
                .OrderBy(x => x.LogicalLevel)
                .ThenBy(x => x.Pathway)
                .ToArray();

            foreach (var level in levels)
            {
                var applicable = officialOutcomes
                    .Where(x =>
                        x.LogicalLevelFrom <= level.LogicalLevel &&
                        x.LogicalLevelTo >= level.LogicalLevel &&
                        PathwayMatches(level.Pathway, x.Pathway))
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Code)
                    .ToArray();

                if (applicable.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"No official Standard/Outcome applies to {pack.Code} logical level {level.LogicalLevel} ({level.NativeLabel}, {level.Pathway ?? "core"}).");
                }

                var pathwayKey = string.IsNullOrWhiteSpace(level.Pathway)
                    ? "CORE"
                    : NormalizeKey(level.Pathway);

                var nativeKey = NormalizeKey(level.NativeLabel);
                var unitCounters = new Dictionary<string, int>(StringComparer.Ordinal);
                var lessonSort = 0;

                foreach (var outcome in applicable)
                {
                    var unit = ResolveTeachingUnit(outcome, nodeById)
                        ?? throw new InvalidOperationException(
                            $"Official node {outcome.Code} has no Domain/Strand/Unit ancestor.");

                    var unitKey =
                        $"{unit.Code}:L{level.LogicalLevel}:{nativeKey}:{pathwayKey}";

                    unitCounters.TryGetValue(unitKey, out var withinUnit);
                    withinUnit++;
                    unitCounters[unitKey] = withinUnit;
                    lessonSort++;

                    var code =
                        $"PED:{pack.Code}:L{level.LogicalLevel}:{nativeKey}:{pathwayKey}:{NormalizeKey(outcome.Code)}";

                    var lessonId = G(
                        $"pedagogical|{state.FrameworkVersionId}|L{level.LogicalLevel}|{nativeKey}|{pathwayKey}|{outcome.Id}");

                    expectedLessons.Add(new CurriculumPedagogicalLesson
                    {
                        Id = lessonId,
                        FrameworkVersionId = state.FrameworkVersionId,
                        OfficialLessonNodeId = null,
                        Code = code,
                        UnitKey = unitKey,
                        UnitTitle = unit.Title,
                        Title = $"{unit.Title} — Lesson {withinUnit:D2}",
                        LogicalLevelFrom = level.LogicalLevel,
                        LogicalLevelTo = level.LogicalLevel,
                        NativeLevel = level.NativeLabel,
                        Pathway = level.Pathway,
                        SortOrder = lessonSort,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });

                    expectedMappings.Add(new CurriculumPedagogicalLessonOutcome
                    {
                        PedagogicalLessonId = lessonId,
                        FrameworkVersionId = state.FrameworkVersionId,
                        OutcomeNodeId = outcome.Id,
                        SortOrder = 1
                    });
                }
            }
        }
    }

    private static CurriculumPackContentNode? ResolveTeachingUnit(
        CurriculumPackContentNode outcome,
        IReadOnlyDictionary<Guid, CurriculumPackContentNode> nodeById)
    {
        var parentId = outcome.ParentId;

        while (parentId.HasValue &&
               nodeById.TryGetValue(parentId.Value, out var parent))
        {
            if (parent.NodeKind is "Domain" or "Strand" or "Unit")
                return parent;

            parentId = parent.ParentId;
        }

        return null;
    }

    private static bool PathwayMatches(
        string? levelPathway,
        string? officialPathway)
    {
        if (string.IsNullOrWhiteSpace(officialPathway))
            return true;

        if (string.IsNullOrWhiteSpace(levelPathway))
            return false;

        var wanted = levelPathway.Trim();
        var exact = officialPathway
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => string.Equals(x, wanted, StringComparison.OrdinalIgnoreCase));

        if (exact)
            return true;

        var officialNormalized = NormalizeKey(officialPathway);
        var wantedNormalized = NormalizeKey(wanted);

        return officialNormalized.Contains(
                   wantedNormalized,
                   StringComparison.Ordinal) ||
               wantedNormalized.Contains(
                   officialNormalized,
                   StringComparison.Ordinal);
    }

    private static void ValidateExpectedGraph(
        IReadOnlyCollection<CurriculumPedagogicalLesson> lessons,
        IReadOnlyCollection<CurriculumPedagogicalLessonOutcome> mappings)
    {
        var duplicateLessonIds = lessons
            .GroupBy(x => x.Id)
            .Where(x => x.Count() != 1)
            .Select(x => x.Key)
            .ToArray();

        if (duplicateLessonIds.Length != 0)
            throw new InvalidOperationException("Duplicate pedagogical lesson ids were generated.");

        var duplicateCodes = lessons
            .GroupBy(
                x => (x.FrameworkVersionId, x.Code),
                EqualityComparer<(Guid, string)>.Default)
            .Where(x => x.Count() != 1)
            .ToArray();

        if (duplicateCodes.Length != 0)
            throw new InvalidOperationException("Duplicate pedagogical lesson codes were generated.");

        var uaeLessonIds = lessons
            .Where(x => x.OfficialLessonNodeId.HasValue)
            .Select(x => x.Id)
            .ToHashSet();

        if (uaeLessonIds.Count != 42)
            throw new InvalidOperationException("UAE pedagogical lesson cardinality drift.");

        var uaeMappings = mappings
            .Count(x => uaeLessonIds.Contains(x.PedagogicalLessonId));

        if (uaeMappings != 48)
            throw new InvalidOperationException("UAE pedagogical alignment cardinality drift.");

        var mappingCountByLesson = mappings
            .GroupBy(x => x.PedagogicalLessonId)
            .ToDictionary(x => x.Key, x => x.Count());

        var nonUaeLessons = lessons
            .Where(x => !x.OfficialLessonNodeId.HasValue)
            .ToArray();

        if (nonUaeLessons.Length == 0 ||
            nonUaeLessons.Any(x => mappingCountByLesson.GetValueOrDefault(x.Id) != 1))
        {
            throw new InvalidOperationException(
                "Every non-UAE pedagogical lesson must map to exactly one applicable official Standard/Outcome.");
        }

        if (mappings.Any(m =>
                lessons.All(l =>
                    l.Id != m.PedagogicalLessonId ||
                    l.FrameworkVersionId != m.FrameworkVersionId)))
        {
            throw new InvalidOperationException(
                "Pedagogical mapping points outside its lesson framework version.");
        }
    }

    private async Task UpsertLessonsAsync(
        IReadOnlyCollection<CurriculumPedagogicalLesson> expected,
        CancellationToken ct)
    {
        var versionIds = expected
            .Select(x => x.FrameworkVersionId)
            .Distinct()
            .ToArray();

        var existing = await _db.CurriculumPedagogicalLessons
            .Where(x => versionIds.Contains(x.FrameworkVersionId))
            .ToArrayAsync(ct);

        var byId = existing.ToDictionary(x => x.Id);

        foreach (var row in expected)
        {
            if (byId.TryGetValue(row.Id, out var current))
            {
                EnsureLessonMatches(current, row);
                continue;
            }

            _db.CurriculumPedagogicalLessons.Add(row);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertMappingsAsync(
        IReadOnlyCollection<CurriculumPedagogicalLessonOutcome> expected,
        CancellationToken ct)
    {
        var lessonIds = expected
            .Select(x => x.PedagogicalLessonId)
            .Distinct()
            .ToArray();

        var existing = await _db.CurriculumPedagogicalLessonOutcomes
            .Where(x => lessonIds.Contains(x.PedagogicalLessonId))
            .ToArrayAsync(ct);

        var byKey = existing.ToDictionary(
            x => (x.PedagogicalLessonId, x.OutcomeNodeId));

        foreach (var row in expected)
        {
            var key = (row.PedagogicalLessonId, row.OutcomeNodeId);

            if (byKey.TryGetValue(key, out var current))
            {
                if (current.FrameworkVersionId != row.FrameworkVersionId ||
                    current.SortOrder != row.SortOrder)
                {
                    throw new InvalidOperationException(
                        $"Pedagogical lesson outcome alignment drift: {key.PedagogicalLessonId}:{key.OutcomeNodeId}.");
                }

                continue;
            }

            _db.CurriculumPedagogicalLessonOutcomes.Add(row);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task RemoveStaleLessonsSafelyAsync(
        IReadOnlyCollection<CurriculumPedagogicalLesson> expected,
        CancellationToken ct)
    {
        var versionIds = expected
            .Select(x => x.FrameworkVersionId)
            .Distinct()
            .ToArray();

        var expectedIds = expected
            .Select(x => x.Id)
            .ToHashSet();

        var allExisting = await _db.CurriculumPedagogicalLessons
            .Where(x => versionIds.Contains(x.FrameworkVersionId))
            .ToArrayAsync(ct);

        var stale = allExisting
            .Where(x => !expectedIds.Contains(x.Id))
            .ToArray();

        if (stale.Length == 0)
            return;

        var staleIds = stale
            .Select(x => x.Id)
            .ToArray();

        var referencedByCanonicalContent =
            await _db.CurriculumLessonContents
                .AsNoTracking()
                .Where(x => staleIds.Contains(x.PedagogicalLessonId))
                .Select(x => x.PedagogicalLessonId)
                .Distinct()
                .ToArrayAsync(ct);

        if (referencedByCanonicalContent.Length != 0)
        {
            throw new InvalidOperationException(
                "Refusing to remove obsolete pseudo-lessons because canonical lesson content references them. " +
                $"Referenced lesson ids: {string.Join(", ", referencedByCanonicalContent)}");
        }

        var staleMappings = await _db.CurriculumPedagogicalLessonOutcomes
            .Where(x => staleIds.Contains(x.PedagogicalLessonId))
            .ToArrayAsync(ct);

        _db.CurriculumPedagogicalLessonOutcomes.RemoveRange(staleMappings);
        _db.CurriculumPedagogicalLessons.RemoveRange(stale);
        await _db.SaveChangesAsync(ct);
    }

    private static void EnsureLessonMatches(
        CurriculumPedagogicalLesson current,
        CurriculumPedagogicalLesson expected)
    {
        if (current.FrameworkVersionId != expected.FrameworkVersionId ||
            current.OfficialLessonNodeId != expected.OfficialLessonNodeId ||
            !string.Equals(current.Code, expected.Code, StringComparison.Ordinal) ||
            !string.Equals(current.UnitKey, expected.UnitKey, StringComparison.Ordinal) ||
            !string.Equals(current.UnitTitle, expected.UnitTitle, StringComparison.Ordinal) ||
            !string.Equals(current.Title, expected.Title, StringComparison.Ordinal) ||
            current.LogicalLevelFrom != expected.LogicalLevelFrom ||
            current.LogicalLevelTo != expected.LogicalLevelTo ||
            !string.Equals(current.NativeLevel, expected.NativeLevel, StringComparison.Ordinal) ||
            !string.Equals(current.Pathway, expected.Pathway, StringComparison.Ordinal) ||
            current.SortOrder != expected.SortOrder)
        {
            throw new InvalidOperationException(
                $"Pedagogical lesson baseline drift: {expected.Code}.");
        }
    }

    private static string NormalizeKey(string value)
    {
        var normalized = Regex.Replace(
            value.Trim().ToUpperInvariant(),
            @"[^A-Z0-9]+",
            "-");

        return normalized.Trim('-');
    }

    private static Guid G(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }
}
