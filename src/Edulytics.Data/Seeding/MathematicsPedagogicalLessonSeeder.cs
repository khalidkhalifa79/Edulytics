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
/// For a curriculum scope with an accepted source-driven pedagogical blueprint,
/// Edulytics seeds the real lesson sequence and only the explicit formal
/// Standard/Outcome targets recorded by that blueprint. A blueprint lesson may
/// therefore have zero, one, or multiple formal mappings.
///
/// Scopes without a resolved blueprint keep the deterministic one-outcome-per-
/// lesson fallback except Cambridge International Mathematics. Cambridge is
/// reference-only until reviewed real pedagogical blueprints exist; synthetic
/// Cambridge fallback lessons are prohibited.
/// No fuzzy text matching or invented official relationship is permitted.
/// </summary>
public sealed class MathematicsPedagogicalLessonSeeder
{
    private static readonly string[] SupportedCodes =
    [
        MathematicsCurriculumPackRegistry.CambridgeCode,
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
        var blueprintLessonIds = new HashSet<Guid>();

        var blueprints =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments();

        await BuildUaeAsync(
            stateByCode[MathematicsCurriculumPackRegistry.UaeCode],
            expectedLessons,
            expectedMappings,
            ct);

        await BuildBlueprintLessonsAsync(
            stateByCode,
            blueprints,
            expectedLessons,
            expectedMappings,
            blueprintLessonIds,
            ct);

        await BuildOutcomeBackedLessonsAsync(
            stateByCode,
            blueprints,
            expectedLessons,
            expectedMappings,
            ct);

        ValidateExpectedGraph(
            expectedLessons,
            expectedMappings,
            blueprintLessonIds);

        await UpsertLessonsAsync(
            expectedLessons,
            ct);

        await UpsertMappingsAsync(
            expectedLessons,
            expectedMappings,
            ct);

        await RemoveStaleLessonsSafelyAsync(
            expectedLessons,
            ct);
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

    private async Task BuildBlueprintLessonsAsync(
        IReadOnlyDictionary<string, CurriculumPackImportState> stateByCode,
        IReadOnlyCollection<PedagogicalLessonBlueprintDocument> blueprints,
        ICollection<CurriculumPedagogicalLesson> expectedLessons,
        ICollection<CurriculumPedagogicalLessonOutcome> expectedMappings,
        ISet<Guid> blueprintLessonIds,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        foreach (var document in blueprints
                     .OrderBy(x => x.PackCode, StringComparer.Ordinal)
                     .ThenBy(
                         x =>
                             x.SchemaVersion == 1
                                 ? x.LogicalLevel
                                 : x.LogicalLevelFrom)
                     .ThenBy(x => x.BlueprintCode, StringComparer.Ordinal))
        {
            PedagogicalLessonBlueprintContract.Validate(document);

            if (!stateByCode.TryGetValue(
                    document.PackCode,
                    out var state))
            {
                throw new InvalidOperationException(
                    $"Blueprint pack is not an accepted Mathematics pack: " +
                    $"{document.PackCode}.");
            }

            if (!string.Equals(
                    state.VersionCode,
                    document.VersionCode,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Blueprint version drift for {document.BlueprintCode}. " +
                    $"Accepted={state.VersionCode}, " +
                    $"blueprint={document.VersionCode}.");
            }

            var outcomeCodes =
                document.Lessons
                    .SelectMany(
                        x =>
                            document.SchemaVersion == 1
                                ? x.OutcomeCodes
                                : x.FormalTargets
                                    .Select(
                                        y =>
                                            y.OutcomeCode))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            var officialNodes =
                await _db.CurriculumPackContentNodes
                    .AsNoTracking()
                    .Where(x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        x.FrameworkCode ==
                            document.PackCode &&
                        outcomeCodes.Contains(x.Code))
                    .ToArrayAsync(ct);

            var officialByCode =
                officialNodes.ToDictionary(
                    x => x.Code,
                    StringComparer.Ordinal);

            if (officialByCode.Count !=
                outcomeCodes.Length)
            {
                var missing =
                    outcomeCodes
                        .Except(
                            officialByCode.Keys,
                            StringComparer.Ordinal)
                        .OrderBy(x => x)
                        .ToArray();

                throw new InvalidOperationException(
                    $"Blueprint {document.BlueprintCode} " +
                    $"references missing official outcomes: " +
                    string.Join(", ", missing));
            }

            foreach (var official in officialNodes)
            {
                if (!official.IsOfficial ||
                    !official.IsActive ||
                    official.NodeKind is not
                        ("Standard" or "Outcome") ||
                    official.LogicalLevelFrom >
                        (
                            document.SchemaVersion == 1
                                ? document.LogicalLevel
                                : document.LogicalLevelTo
                        ) ||
                    official.LogicalLevelTo <
                        (
                            document.SchemaVersion == 1
                                ? document.LogicalLevel
                                : document.LogicalLevelFrom
                        ))
                {
                    throw new InvalidOperationException(
                        $"Blueprint {document.BlueprintCode} " +
                        $"resolved invalid official outcome: " +
                        $"{official.Code}.");
                }
            }

            foreach (var lesson in document.Lessons
                         .OrderBy(x => x.SortOrder))
            {
                var lessonId =
                    G(
                        $"pedagogical-blueprint|" +
                        $"{state.FrameworkVersionId}|" +
                        $"{document.BlueprintCode}|" +
                        $"{lesson.SourceLessonCode}");

                if (!blueprintLessonIds.Add(
                        lessonId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate deterministic blueprint lesson id: " +
                        $"{lesson.LessonCode}.");
                }

                expectedLessons.Add(
                    new CurriculumPedagogicalLesson
                    {
                        Id = lessonId,
                        FrameworkVersionId =
                            state.FrameworkVersionId,
                        OfficialLessonNodeId = null,
                        Code = lesson.LessonCode,
                        UnitKey =
                            document.SchemaVersion == 1
                                ? $"{document.BlueprintCode}:" +
                                  $"U{lesson.UnitNumber:D2}"
                                : $"{document.BlueprintCode}:" +
                                  document.Units
                                      .Single(
                                          x =>
                                              x.Number ==
                                              lesson.UnitNumber)
                                      .UnitCode,
                        UnitTitle =
                            lesson.UnitTitle,
                        Title =
                            lesson.Title,
                        LogicalLevelFrom =
                            document.SchemaVersion == 1
                                ? document.LogicalLevel
                                : document.LogicalLevelFrom,
                        LogicalLevelTo =
                            document.SchemaVersion == 1
                                ? document.LogicalLevel
                                : document.LogicalLevelTo,
                        NativeLevel =
                            document.NativeLevel,
                        Pathway =
                            document.Pathway,
                        SortOrder =
                            lesson.SortOrder,
                        CreatedAtUtc =
                            now,
                        UpdatedAtUtc =
                            now
                    });

                IEnumerable<
                    (string OutcomeCode, int SortOrder)>
                    formalTargets =
                        document.SchemaVersion == 1
                            ? lesson.OutcomeCodes
                                .Select(
                                    (
                                        outcomeCode,
                                        index
                                    ) =>
                                        (
                                            OutcomeCode:
                                                outcomeCode,
                                            SortOrder:
                                                index + 1
                                        ))
                            : lesson.FormalTargets
                                .OrderBy(
                                    x => x.SortOrder)
                                .Select(
                                    x =>
                                        (
                                            OutcomeCode:
                                                x.OutcomeCode,
                                            SortOrder:
                                                x.SortOrder
                                        ));

                foreach (var target in formalTargets)
                {
                    expectedMappings.Add(
                        new CurriculumPedagogicalLessonOutcome
                        {
                            PedagogicalLessonId =
                                lessonId,
                            FrameworkVersionId =
                                state.FrameworkVersionId,
                            OutcomeNodeId =
                                officialByCode[
                                    target.OutcomeCode].Id,
                            SortOrder =
                                target.SortOrder
                        });
                }
            }
        }
    }

    private async Task BuildOutcomeBackedLessonsAsync(
        IReadOnlyDictionary<string, CurriculumPackImportState> stateByCode,
        IReadOnlyCollection<PedagogicalLessonBlueprintDocument> blueprints,
        ICollection<CurriculumPedagogicalLesson> expectedLessons,
        ICollection<CurriculumPedagogicalLessonOutcome> expectedMappings,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        foreach (var pack in MathematicsCurriculumPackRegistry.All
                     .Where(
                         x =>
                             x.Code !=
                                 MathematicsCurriculumPackRegistry.UaeCode &&
                             x.Code !=
                                 MathematicsCurriculumPackRegistry.CambridgeCode))
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
                // Edulytics Mathematics product scope begins at Grade 1.
                //
                // Common Core Kindergarten remains in the verified official
                // framework for source integrity, but no Kindergarten
                // pedagogical product lessons are seeded.
                //
                // RemoveStaleLessonsSafelyAsync retires historical fallback
                // lessons and refuses removal if canonical content references
                // any stale lesson.
                if (
                    string.Equals(
                        pack.Code,
                        MathematicsCurriculumPackRegistry.CommonCoreCode,
                        StringComparison.Ordinal) &&
                    level.LogicalLevel == 1 &&
                    string.Equals(
                        level.NativeLabel,
                        "Kindergarten",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var ownedByBlueprint =
                    blueprints.Any(
                        x =>
                            string.Equals(
                                x.PackCode,
                                pack.Code,
                                StringComparison.Ordinal) &&
                            string.Equals(
                                x.VersionCode,
                                state.VersionCode,
                                StringComparison.Ordinal) &&
                            (
                                (
                                    x.SchemaVersion == 1 &&
                                    x.LogicalLevel ==
                                        level.LogicalLevel &&
                                    string.Equals(
                                        x.NativeLevel,
                                        level.NativeLabel,
                                        StringComparison.Ordinal) &&
                                    string.Equals(
                                        x.Pathway,
                                        level.Pathway,
                                        StringComparison.Ordinal)
                                )
                                ||
                                (
                                    x.SchemaVersion == 2 &&
                                    x.SuppressOutcomeFallbackForLogicalRange &&
                                    level.LogicalLevel >=
                                        x.LogicalLevelFrom &&
                                    level.LogicalLevel <=
                                        x.LogicalLevelTo
                                )
                            ));

                if (ownedByBlueprint)
                {
                    continue;
                }

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
        IReadOnlyCollection<CurriculumPedagogicalLessonOutcome> mappings,
        IReadOnlySet<Guid> blueprintLessonIds)
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

        var blueprintLessons =
            nonUaeLessons
                .Where(
                    x =>
                        blueprintLessonIds.Contains(
                            x.Id))
                .ToArray();

        if (blueprintLessons.Length !=
            blueprintLessonIds.Count)
        {
            throw new InvalidOperationException(
                "Blueprint lesson identity/cardinality drift.");
        }

        var fallbackLessons =
            nonUaeLessons
                .Where(
                    x =>
                        !blueprintLessonIds.Contains(
                            x.Id))
                .ToArray();

        if (fallbackLessons.Any(
                x =>
                    mappingCountByLesson
                        .GetValueOrDefault(
                            x.Id) != 1))
        {
            throw new InvalidOperationException(
                "Every fallback non-UAE pedagogical lesson must map " +
                "to exactly one applicable official Standard/Outcome.");
        }

        var duplicateMappings =
            mappings
                .GroupBy(
                    x => (
                        x.PedagogicalLessonId,
                        x.OutcomeNodeId))
                .Where(x => x.Count() != 1)
                .ToArray();

        if (duplicateMappings.Length != 0)
        {
            throw new InvalidOperationException(
                "Duplicate pedagogical lesson outcome mapping generated.");
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
                if (!TryUpgradeAcceptedCommonCoreGrade1B3Lesson(
                        current,
                        row))
                {
                    EnsureLessonMatches(
                        current,
                        row);
                }

                continue;
            }

            _db.CurriculumPedagogicalLessons.Add(row);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertMappingsAsync(
        IReadOnlyCollection<CurriculumPedagogicalLesson> expectedLessons,
        IReadOnlyCollection<CurriculumPedagogicalLessonOutcome> expected,
        CancellationToken ct)
    {
        var lessonIds =
            expectedLessons
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

        var existing =
            await _db.CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        lessonIds.Contains(
                            x.PedagogicalLessonId))
                .ToArrayAsync(ct);

        var expectedKeys =
            expected
                .Select(
                    x => (
                        x.PedagogicalLessonId,
                        x.OutcomeNodeId))
                .ToHashSet();

        var unexpected =
            existing
                .Where(
                    x =>
                        !expectedKeys.Contains(
                            (
                                x.PedagogicalLessonId,
                                x.OutcomeNodeId
                            )))
                .ToArray();

        if (unexpected.Length != 0)
        {
            throw new InvalidOperationException(
                "Unexpected existing pedagogical outcome alignment drift: " +
                string.Join(
                    ", ",
                    unexpected.Select(
                        x =>
                            $"{x.PedagogicalLessonId}:" +
                            $"{x.OutcomeNodeId}")));
        }

        var byKey =
            existing.ToDictionary(
                x => (
                    x.PedagogicalLessonId,
                    x.OutcomeNodeId));

        foreach (var row in expected)
        {
            var key =
                (
                    row.PedagogicalLessonId,
                    row.OutcomeNodeId
                );

            if (byKey.TryGetValue(
                    key,
                    out var current))
            {
                if (current.FrameworkVersionId !=
                        row.FrameworkVersionId ||
                    current.SortOrder !=
                        row.SortOrder)
                {
                    throw new InvalidOperationException(
                        $"Pedagogical lesson outcome alignment drift: " +
                        $"{key.PedagogicalLessonId}:" +
                        $"{key.OutcomeNodeId}.");
                }

                continue;
            }

            _db.CurriculumPedagogicalLessonOutcomes
                .Add(row);
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

    private static bool TryUpgradeAcceptedCommonCoreGrade1B3Lesson(
        CurriculumPedagogicalLesson current,
        CurriculumPedagogicalLesson expected)
    {
        const string grade1Prefix =
            "PED:US-CCSS-MATH:L2:GRADE-1:CORE:";

        if (!expected.Code.StartsWith(
                grade1Prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        var oaAffected =
            expected.Code.EndsWith(
                ":CCSS-1-OA-B-4",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-1-OA-C-5",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-1-OA-C-6",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-1-OA-D-7",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-1-OA-D-8",
                StringComparison.Ordinal);

        var practiceAffected =
            expected.Code.EndsWith(
                ":CCSS-MP-1",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-2",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-3",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-4",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-5",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-6",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-7",
                StringComparison.Ordinal) ||
            expected.Code.EndsWith(
                ":CCSS-MP-8",
                StringComparison.Ordinal);

        if (!oaAffected &&
            !practiceAffected)
        {
            return false;
        }

        if (current.FrameworkVersionId !=
                expected.FrameworkVersionId ||
            current.OfficialLessonNodeId !=
                expected.OfficialLessonNodeId ||
            !string.Equals(
                current.Code,
                expected.Code,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.UnitKey,
                expected.UnitKey,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.UnitTitle,
                expected.UnitTitle,
                StringComparison.Ordinal) ||
            current.LogicalLevelFrom !=
                expected.LogicalLevelFrom ||
            current.LogicalLevelTo !=
                expected.LogicalLevelTo ||
            !string.Equals(
                current.NativeLevel,
                expected.NativeLevel,
                StringComparison.Ordinal) ||
            !string.Equals(
                current.Pathway,
                expected.Pathway,
                StringComparison.Ordinal) ||
            current.SortOrder !=
                expected.SortOrder - 1)
        {
            return false;
        }

        if (oaAffected)
        {
            const string marker =
                " — Lesson ";

            var markerIndex =
                expected.Title.LastIndexOf(
                    marker,
                    StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                return false;
            }

            var numberStart =
                markerIndex +
                marker.Length;

            var currentNumberText =
                expected.Title[
                    numberStart..];

            if (!int.TryParse(
                    currentNumberText,
                    out var currentNumber) ||
                currentNumber <= 1)
            {
                return false;
            }

            var historicalTitle =
                expected.Title[
                    ..numberStart] +
                (currentNumber - 1)
                    .ToString("D2");

            if (!string.Equals(
                    current.Title,
                    historicalTitle,
                    StringComparison.Ordinal))
            {
                return false;
            }

            current.Title =
                expected.Title;
        }
        else
        {
            // Adding 1.OA.B.3 increases the Grade 1 global
            // fallback lesson SortOrder before the eight
            // Mathematical Practices. Their unit-local titles
            // remain unchanged.
            if (!string.Equals(
                    current.Title,
                    expected.Title,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        current.SortOrder =
            expected.SortOrder;

        current.UpdatedAtUtc =
            DateTime.UtcNow;

        return true;
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
