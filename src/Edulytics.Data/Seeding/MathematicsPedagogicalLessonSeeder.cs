using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Seeds platform-scoped Edulytics pedagogical lesson definitions without adding
/// synthetic Unit/Lesson nodes to the verified official curriculum packs.
///
/// UAE: each verified official Lesson node becomes the pedagogical lesson identity,
/// preserving the official lesson node id and verified LessonStandardAlignment links.
///
/// England/Common Core/Poland: the existing Edulytics MathematicsLessonBlueprintRegistry
/// supplies baseline lesson slots. These are Edulytics pedagogical definitions, not
/// official curriculum Lesson nodes. Official standard/outcome alignments are curated
/// separately; this seeder never guesses them.
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
        MathematicsLessonBlueprintRegistry.Validate();

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

        BuildBlueprintLessons(
            stateByCode,
            expectedLessons);

        if (expectedLessons.Count != 313)
        {
            throw new InvalidOperationException(
                $"Pedagogical Mathematics baseline count drift. Expected 313, got {expectedLessons.Count}.");
        }

        if (expectedMappings.Count != 48)
        {
            throw new InvalidOperationException(
                $"Verified UAE pedagogical alignment count drift. Expected 48, got {expectedMappings.Count}.");
        }

        await UpsertLessonsAsync(expectedLessons, ct);
        await UpsertMappingsAsync(expectedMappings, ct);
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
            throw new InvalidOperationException("UAE pedagogical seed requires exactly 42 verified official Lesson nodes.");

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
                // Deliberately reuse the verified official Lesson id so any canonical
                // content row created by the prior Phase29 architecture remains valid.
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
            throw new InvalidOperationException("UAE pedagogical seed requires exactly 48 verified LessonStandardAlignment links.");

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

    private static void BuildBlueprintLessons(
        IReadOnlyDictionary<string, CurriculumPackImportState> stateByCode,
        ICollection<CurriculumPedagogicalLesson> expectedLessons)
    {
        var allBlueprints = MathematicsLessonBlueprintRegistry.CreateBlueprints();
        var now = DateTime.UtcNow;

        foreach (var pack in MathematicsCurriculumPackRegistry.All
                     .Where(x => x.Code != MathematicsCurriculumPackRegistry.UaeCode))
        {
            var state = stateByCode[pack.Code];

            foreach (var level in pack.Levels
                         .GroupBy(x => new { x.LogicalLevel, x.NativeLabel, x.Pathway })
                         .Select(x => x.First()))
            {
                var units = allBlueprints
                    .Where(x =>
                        x.PackCode == pack.Code &&
                        x.LogicalLevel == level.LogicalLevel &&
                        string.Equals(
                            x.NativeLevel,
                            level.NativeLabel,
                            StringComparison.Ordinal))
                    .GroupBy(x => x.UnitKey, StringComparer.Ordinal)
                    .Select(x => x.First())
                    .ToArray();

                if (units.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"No Edulytics lesson blueprints exist for {pack.Code} logical level {level.LogicalLevel}.");
                }

                for (var index = 0; index < units.Length; index++)
                {
                    var blueprint = units[index];
                    var unitToken = blueprint.UnitKey
                        .Split(':', StringSplitOptions.RemoveEmptyEntries)
                        .Last();

                    var pathwayKey = string.IsNullOrWhiteSpace(level.Pathway)
                        ? "CORE"
                        : NormalizeKey(level.Pathway);

                    var nativeKey = NormalizeKey(level.NativeLabel);
                    var code =
                        $"PED:{pack.Code}:L{level.LogicalLevel}:{nativeKey}:{pathwayKey}:{unitToken}:LESSON-01";

                    expectedLessons.Add(new CurriculumPedagogicalLesson
                    {
                        Id = G($"pedagogical|{state.FrameworkVersionId}|{code}"),
                        FrameworkVersionId = state.FrameworkVersionId,
                        OfficialLessonNodeId = null,
                        Code = code,
                        UnitKey =
                            $"{pack.Code}:L{level.LogicalLevel}:{nativeKey}:{pathwayKey}:{unitToken}",
                        UnitTitle = HumanizeUnit(unitToken),
                        Title = $"{level.NativeLabel} — {HumanizeUnit(unitToken)}",
                        LogicalLevelFrom = level.LogicalLevel,
                        LogicalLevelTo = level.LogicalLevel,
                        NativeLevel = level.NativeLabel,
                        Pathway = level.Pathway,
                        SortOrder = index + 1,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                }
            }
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

    private static string HumanizeUnit(string token)
    {
        var value = Regex.Replace(token, "([a-z0-9])([A-Z])", "$1 $2");
        value = value.Replace('_', ' ').Trim();

        return value.Length == 0
            ? token
            : char.ToUpperInvariant(value[0]) + value[1..];
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
