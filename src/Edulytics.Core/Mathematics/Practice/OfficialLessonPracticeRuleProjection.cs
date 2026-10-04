using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Edulytics.Core.Curriculum;

namespace Edulytics.Core.Mathematics.Practice;

/// <summary>
/// Fail-closed projection for official/outcome-mapped lessons whose canonical
/// title full-matches one reviewed anchored Practice target rule. Broad
/// keyword-only rules are deliberately excluded from this path.
/// </summary>
internal static class OfficialLessonPracticeRuleProjection
{
    private const string SkillResource =
        "Edulytics.Core.Mathematics.Skills.skill-registry.v1.json";
    private const string FamilyResource =
        "Edulytics.Core.Mathematics.Generation.question-family-registry.v1.json";

    public const string ContractVersion = "official-outcome-rules-v3";

    public static IReadOnlyList<LessonPracticeContract> Load()
    {
        try
        {
            using var skills = LoadDocument(SkillResource);
            using var families = LoadDocument(FamilyResource);
            if (skills is null || families is null)
                return [];

            var skillIds = skills.RootElement
                .GetProperty("skills")
                .EnumerateArray()
                .Select(row => row.GetProperty("id").GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToHashSet(StringComparer.Ordinal);

            var familyById = families.RootElement
                .GetProperty("families")
                .EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("id").GetString()!,
                    row => new FamilyRouting(
                        row.GetProperty("skillId").GetString() ?? string.Empty,
                        row.TryGetProperty("lessonPracticeRouting", out var routing) &&
                            routing.ValueKind == JsonValueKind.True,
                        row.TryGetProperty("verificationPolicy", out var verification) &&
                            !string.IsNullOrWhiteSpace(verification.GetString())),
                    StringComparer.Ordinal);

            var projected = new List<LessonPracticeContract>();
            foreach (var pack in LoadContentPacks())
            {
                foreach (var lesson in pack.Lessons)
                {
                    if (string.Equals(
                            pack.PackCode,
                            "PL-NATIONAL-MATH",
                            StringComparison.Ordinal))
                    {
                        if (lesson.OutcomeCodes.Count == 0)
                            continue;

                        var polishMappings = lesson.OutcomeCodes
                            .Select(code =>
                                PolishOutcomePracticeMapRegistry.TryResolve(
                                    code,
                                    out var mapping)
                                    ? mapping
                                    : null)
                            .ToArray();

                        if (!polishMappings.All(x => x is not null))
                            continue;

                        var polishRules = polishMappings
                            .Cast<PolishOutcomePracticeMapping>()
                            .SelectMany(x => x.TargetRules)
                            .DistinctBy(x => x.Id, StringComparer.Ordinal)
                            .ToArray();

                        if (polishRules.Length == 0 ||
                            !polishRules.All(rule =>
                                IsRuntimeReadyRule(
                                    rule,
                                    skillIds,
                                    familyById)))
                        {
                            continue;
                        }

                        var polishSkills = polishRules
                            .Select(rule => rule.SkillId)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(value => value, StringComparer.Ordinal)
                            .ToArray();
                        var polishFamilies = polishRules
                            .SelectMany(rule => rule.Families)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(value => value, StringComparer.Ordinal)
                            .ToArray();
                        var polishMechanic = polishRules.Length == 1
                            ? polishRules[0].Mechanic
                            : "POLISH_OFFICIAL_MULTI_TARGET";

                        projected.Add(new LessonPracticeContract(
                            lesson.LessonCode,
                            polishSkills[0],
                            polishMechanic,
                            polishFamilies,
                            "PolishOfficialOutcomeMap",
                            "READY_VERIFIED",
                            ContractVersion)
                        {
                            SkillIds = polishSkills
                        });
                        continue;
                    }

                    if (string.Equals(
                            pack.PackCode,
                            MathematicsCurriculumPackRegistry.UaeCode,
                            StringComparison.Ordinal) &&
                        TryResolveUaeAdvancedUnit5ExactPractice(
                            lesson.LessonCode,
                            out var uaeUnit5Contract) &&
                        uaeUnit5Contract is not null)
                    {
                        projected.Add(uaeUnit5Contract);
                        continue;
                    }

                    var hasOutcomeCodes = lesson.OutcomeCodes.Count > 0;
                    var isVerifiedUaeTextbookReference =
                        string.Equals(
                            pack.PackCode,
                            MathematicsCurriculumPackRegistry.UaeCode,
                            StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(
                            lesson.OfficialReferenceCode);

                    if (!hasOutcomeCodes &&
                        !isVerifiedUaeTextbookReference)
                    {
                        continue;
                    }

                    var translation = ChooseTranslation(pack, lesson);
                    if (translation is not null &&
                        SupportingPracticeTargetRuleRegistry.TryResolveReviewedOfficialLesson(
                            lesson.LessonCode,
                            translation.Title,
                            translation.Explanation,
                            translation.KeyConceptsAndRules,
                            translation.WorkedExamples,
                            out var canonicalRule) &&
                        canonicalRule is not null &&
                        IsRuntimeReadyRule(canonicalRule, skillIds, familyById))
                    {
                        projected.Add(new LessonPracticeContract(
                            lesson.LessonCode,
                            canonicalRule.SkillId,
                            canonicalRule.Mechanic,
                            canonicalRule.Families
                                .Distinct(StringComparer.Ordinal)
                                .ToArray(),
                            "OfficialReviewedCanonicalLessonRule",
                            "READY_VERIFIED",
                            ContractVersion)
                        {
                            SkillIds = [canonicalRule.SkillId]
                        });
                        continue;
                    }

                    if (!hasOutcomeCodes)
                        continue;

                    var resolvedOutcomes = lesson.OutcomeCodes
                        .Select(code =>
                            OfficialOutcomePracticeRuleRegistry.TryResolve(
                                code,
                                out var resolution)
                                ? resolution
                                : null)
                        .ToArray();

                    if (!resolvedOutcomes.All(x => x is not null))
                        continue;

                    var targetRules = resolvedOutcomes
                        .Cast<OfficialOutcomePracticeResolution>()
                        .Select(x => x.TargetRule)
                        .DistinctBy(x => x.Id, StringComparer.Ordinal)
                        .ToArray();

                    if (targetRules.Length == 0 ||
                        !targetRules.All(rule =>
                            IsRuntimeReadyRule(
                                rule,
                                skillIds,
                                familyById)))
                    {
                        continue;
                    }

                    var skillsForLesson = targetRules
                        .Select(rule => rule.SkillId)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    var familiesForLesson = targetRules
                        .SelectMany(rule => rule.Families)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    var mechanic = targetRules.Length == 1
                        ? targetRules[0].Mechanic
                        : "OFFICIAL_MULTI_OUTCOME";

                    projected.Add(new LessonPracticeContract(
                        lesson.LessonCode,
                        skillsForLesson[0],
                        mechanic,
                        familiesForLesson,
                        "OfficialOutcomeRule",
                        "READY_VERIFIED",
                        ContractVersion)
                    {
                        SkillIds = skillsForLesson
                    });
                }
            }

            var resolved = new List<LessonPracticeContract>();
            foreach (var group in projected
                         .GroupBy(x => x.LessonCode, StringComparer.Ordinal)
                         .OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var first = group.First();
                var conflicts = group
                    .Skip(1)
                    .Where(candidate => !Equivalent(first, candidate))
                    .ToArray();

                if (conflicts.Length != 0)
                {
                    throw new InvalidOperationException(
                        $"Conflicting official Practice projections for {group.Key}.");
                }

                resolved.Add(first);
            }

            return resolved.ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Official lesson Practice projection failed while reading embedded JSON.",
                ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Official lesson Practice projection failed while building exact contracts.",
                ex);
        }
    }

    private static bool IsRuntimeReadyRule(
        SupportingPracticeTargetRule rule,
        IReadOnlySet<string> skillIds,
        IReadOnlyDictionary<string, FamilyRouting> familyById) =>
        skillIds.Contains(rule.SkillId) &&
        rule.Families.Count > 0 &&
        rule.Families.All(familyId =>
            familyById.TryGetValue(familyId, out var family) &&
            family.LessonPracticeRouting &&
            family.HasVerificationPolicy &&
            string.Equals(
                family.SkillId,
                rule.SkillId,
                StringComparison.Ordinal));

    private static IEnumerable<CanonicalLessonContentPackDocument> LoadContentPacks()
    {
        var assembly = typeof(OfficialLessonPracticeRuleProjection).Assembly;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        foreach (var resource in assembly.GetManifestResourceNames()
                     .Where(name => name.EndsWith(
                         ".lesson-content-pack.json",
                         StringComparison.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
                continue;
            var document = JsonSerializer.Deserialize<CanonicalLessonContentPackDocument>(
                stream,
                options);
            if (document is not null)
                yield return document;
        }
    }

    private static bool TryResolveUaeAdvancedUnit5ExactPractice(
        string lessonCode,
        out LessonPracticeContract? contract)
    {
        contract = lessonCode switch
        {
            "PED:UAE-MOE-MATH:L11:ADVANCED:05:01:MULTIVARIABLE-LINEAR-SYSTEMS-AND-ELEMENTARY-ROW-OPERATIONS" =>
                new LessonPracticeContract(
                    lessonCode,
                    "supporting.algebra.equations",
                    "SIMULTANEOUS",
                    ["supporting.algebra.simultaneous"],
                    "OfficialReviewedCanonicalLessonRule",
                    "READY_VERIFIED",
                    ContractVersion),
            "PED:UAE-MOE-MATH:L11:ADVANCED:05:03:SOLVING-LINEAR-SYSTEMS-USING-INVERSES-AND-CRAMER-S-RULE" =>
                new LessonPracticeContract(
                    lessonCode,
                    "supporting.algebra.equations",
                    "SIMULTANEOUS",
                    ["supporting.algebra.simultaneous"],
                    "OfficialReviewedCanonicalLessonRule",
                    "READY_VERIFIED",
                    ContractVersion),
            "PED:UAE-MOE-MATH:L11:ADVANCED:05:04:PARTIAL-FRACTIONS" =>
                new LessonPracticeContract(
                    lessonCode,
                    "supporting.algebra.expressions",
                    "ALGEBRAIC_FRACTION",
                    ["supporting.algebra.algebraic_fraction"],
                    "OfficialReviewedCanonicalLessonRule",
                    "READY_VERIFIED",
                    ContractVersion),
            "PED:UAE-MOE-MATH:L11:ADVANCED:05:05:LINEAR-PROGRAMMING" =>
                new LessonPracticeContract(
                    lessonCode,
                    "supporting.algebra.systems_inequalities",
                    "LINEAR_PROGRAMMING",
                    ["supporting.algebra.linear_programming.vertex_optimum"],
                    "OfficialReviewedCanonicalLessonRule",
                    "READY_VERIFIED",
                    ContractVersion),
            _ => null
        };

        if (contract is null)
            return false;

        contract = contract with
        {
            SkillIds = [contract.SkillId]
        };
        return true;
    }

    private static bool Equivalent(
        LessonPracticeContract left,
        LessonPracticeContract right) =>
        string.Equals(left.LessonCode, right.LessonCode, StringComparison.Ordinal) &&
        string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal) &&
        string.Equals(left.Mechanic, right.Mechanic, StringComparison.Ordinal) &&
        string.Equals(left.SourceType, right.SourceType, StringComparison.Ordinal) &&
        string.Equals(left.Readiness, right.Readiness, StringComparison.Ordinal) &&
        string.Equals(left.ContractVersion, right.ContractVersion, StringComparison.Ordinal) &&
        left.SkillIds.SequenceEqual(right.SkillIds, StringComparer.Ordinal) &&
        left.AllowedQuestionFamilies.SequenceEqual(
            right.AllowedQuestionFamilies,
            StringComparer.Ordinal);

    private static CanonicalLessonContentPackTranslation? ChooseTranslation(
        CanonicalLessonContentPackDocument pack,
        CanonicalLessonContentPackLesson lesson) =>
        lesson.Translations.FirstOrDefault(x =>
            string.Equals(
                x.CultureCode,
                pack.AcademicLanguage,
                StringComparison.OrdinalIgnoreCase))
        ?? lesson.Translations.FirstOrDefault(x =>
            x.CultureCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        ?? lesson.Translations.FirstOrDefault();

    private static JsonDocument? LoadDocument(string logicalName)
    {
        var stream = typeof(OfficialLessonPracticeRuleProjection)
            .Assembly
            .GetManifestResourceStream(logicalName);
        if (stream is null)
            return null;
        using (stream)
            return JsonDocument.Parse(stream);
    }

    private sealed record FamilyRouting(
        string SkillId,
        bool LessonPracticeRouting,
        bool HasVerificationPolicy);
}
