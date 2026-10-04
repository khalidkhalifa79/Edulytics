using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Edulytics.Core.Mathematics.Practice;

public sealed record SupportingPracticeContentRecipe(
    string Concept,
    string WorkedExample,
    string Solution,
    string CommonMistake,
    string Summary);

public sealed record SupportingPracticeTargetRule(
    string Id,
    IReadOnlyList<string> TitlePatterns,
    IReadOnlyList<string> CodePatterns,
    string SkillId,
    string Mechanic,
    IReadOnlyList<string> Families,
    SupportingPracticeContentRecipe Content);

/// <summary>
/// Reviewed, ordered Supporting-lesson target rules. This registry is shared by
/// runtime Practice projection, canonical content remediation and deterministic
/// audits so those three surfaces cannot silently disagree.
/// </summary>
public static class SupportingPracticeTargetRuleRegistry
{
    private const string ResourceName =
        "Edulytics.Core.Mathematics.Curriculum.supporting-practice-target-rules.v1.json";

    private static readonly Lazy<IReadOnlyList<CompiledRule>> Rules = new(Load);

    public static IReadOnlyList<SupportingPracticeTargetRule> All =>
        Rules.Value.Select(x => x.Rule).ToArray();

    public static bool TryGetById(
        string? ruleId,
        out SupportingPracticeTargetRule? rule)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            rule = null;
            return false;
        }

        rule = Rules.Value
            .Select(candidate => candidate.Rule)
            .SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    ruleId.Trim(),
                    StringComparison.Ordinal));
        return rule is not null;
    }

    public static bool TryResolve(
        string? lessonCode,
        string? title,
        out SupportingPracticeTargetRule? rule)
    {
        var code = (lessonCode ?? string.Empty).Trim();
        var normalizedTitle = NormalizeTitle(title);

        foreach (var candidate in Rules.Value)
        {
            var titleMatch = candidate.TitlePatterns.Count == 0 ||
                candidate.TitlePatterns.Any(pattern => pattern.IsMatch(normalizedTitle));
            if (!titleMatch)
                continue;

            var codeMatch = candidate.CodePatterns.Count == 0 ||
                candidate.CodePatterns.Any(pattern => pattern.IsMatch(code));
            if (!codeMatch)
                continue;

            rule = candidate.Rule;
            return true;
        }

        rule = null;
        return false;
    }

    /// <summary>
    /// Resolves only a reviewed rule whose title pattern is explicitly anchored
    /// from start to end. This is the only rule-based promotion path permitted
    /// for official/outcome-mapped lessons; broad keyword patterns remain
    /// Supporting-only and cannot authorize official Practice.
    /// </summary>
    public static bool TryResolveReviewedExactTitle(
        string? lessonCode,
        string? title,
        out SupportingPracticeTargetRule? rule)
    {
        var code = (lessonCode ?? string.Empty).Trim();
        var normalizedTitle = NormalizeTitle(title);
        var matches = Rules.Value
            .Where(candidate =>
                candidate.TitlePatterns.Any(pattern =>
                    IsReviewedExactTitlePattern(pattern.ToString()) &&
                    pattern.IsMatch(normalizedTitle)) &&
                (candidate.CodePatterns.Count == 0 ||
                 candidate.CodePatterns.Any(pattern => pattern.IsMatch(code))))
            .Select(candidate => candidate.Rule)
            .DistinctBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 1)
        {
            rule = matches[0];
            return true;
        }

        rule = null;
        return false;
    }

    private static bool IsReviewedExactTitlePattern(string pattern)
    {
        var value = pattern.Trim();
        return value.StartsWith("^", StringComparison.Ordinal) &&
               value.EndsWith("$", StringComparison.Ordinal);
    }

    private static bool TryResolveReviewedExactCode(
        string? lessonCode,
        out SupportingPracticeTargetRule? rule)
    {
        var code = (lessonCode ?? string.Empty).Trim();
        if (code.Length == 0)
        {
            rule = null;
            return false;
        }

        var matches = Rules.Value
            .Where(candidate =>
                candidate.TitlePatterns.Count == 0 &&
                candidate.CodePatterns.Any(pattern =>
                    IsReviewedExactTitlePattern(pattern.ToString()) &&
                    pattern.IsMatch(code)))
            .Select(candidate => candidate.Rule)
            .DistinctBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 1)
        {
            rule = matches[0];
            return true;
        }

        rule = null;
        return false;
    }

    /// <summary>
    /// Resolves an official lesson only when the reviewed target-rule registry
    /// yields one unambiguous target. An anchored exact-title match wins when
    /// unique; otherwise the complete reviewed rule set must still produce one
    /// and only one candidate. Ambiguity always fails closed.
    /// </summary>
    public static bool TryResolveReviewedOfficialTitle(
        string? lessonCode,
        string? title,
        out SupportingPracticeTargetRule? rule)
    {
        if (TryResolveReviewedExactCode(lessonCode, out rule))
            return true;

        if (TryResolveReviewedExactTitle(lessonCode, title, out rule))
            return true;

        var code = (lessonCode ?? string.Empty).Trim();
        var normalizedTitle = NormalizeTitle(title);
        var matches = Rules.Value
            .Where(candidate =>
                (candidate.TitlePatterns.Count == 0 ||
                 candidate.TitlePatterns.Any(pattern => pattern.IsMatch(normalizedTitle))) &&
                (candidate.CodePatterns.Count == 0 ||
                 candidate.CodePatterns.Any(pattern => pattern.IsMatch(code))))
            .Select(candidate => candidate.Rule)
            .DistinctBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 1)
        {
            rule = matches[0];
            return true;
        }

        rule = null;
        return false;
    }

    /// <summary>
    /// Resolves an official lesson from reviewed target rules using the canonical
    /// learner-facing body only when that evidence yields one unambiguous target.
    /// Title-only reviewed resolution remains preferred. Canonical-body matching
    /// is a second fail-closed path; collisions never authorize Practice.
    /// </summary>
    public static bool TryResolveReviewedOfficialLesson(
        string? lessonCode,
        string? title,
        string? explanation,
        string? keyConcepts,
        string? workedExamples,
        out SupportingPracticeTargetRule? rule)
    {
        if (TryResolveReviewedOfficialTitle(lessonCode, title, out rule))
            return true;

        var code = (lessonCode ?? string.Empty).Trim();
        var evidence = Regex.Replace(
            string.Join(
                " ",
                NormalizeTitle(title),
                explanation ?? string.Empty,
                keyConcepts ?? string.Empty,
                workedExamples ?? string.Empty),
            @"\s+",
            " ").Trim();

        var matches = Rules.Value
            .Where(candidate =>
                (candidate.TitlePatterns.Count == 0 ||
                 candidate.TitlePatterns.Any(pattern => pattern.IsMatch(evidence))) &&
                (candidate.CodePatterns.Count == 0 ||
                 candidate.CodePatterns.Any(pattern => pattern.IsMatch(code))))
            .Select(candidate => candidate.Rule)
            .DistinctBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 1)
        {
            rule = matches[0];
            return true;
        }

        rule = null;
        return false;
    }

    public static string NormalizeTitle(string? title)
    {
        var value = Regex.Replace(
            title ?? string.Empty,
            @"\s*[—-]\s*advanced reasoning\s*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        value = Regex.Replace(
            value,
            @":\s*(?:build the idea|reason and apply)\s*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        value = Regex.Replace(
            value,
            @"^\s*consolidating\s+",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static IReadOnlyList<CompiledRule> Load()
    {
        var assembly = typeof(SupportingPracticeTargetRuleRegistry).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded Supporting Practice target rule registry: {ResourceName}.");

        using var document = JsonDocument.Parse(stream);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CompiledRule>();

        foreach (var row in document.RootElement.GetProperty("rules").EnumerateArray())
        {
            var id = Required(row, "id");
            if (!seen.Add(id))
                throw new InvalidOperationException($"Duplicate Supporting Practice rule id: {id}.");

            var titlePatterns = ReadList(row, "titlePatterns");
            var codePatterns = ReadList(row, "codePatterns");
            if (titlePatterns.Count == 0 && codePatterns.Count == 0)
                throw new InvalidOperationException($"Supporting Practice rule {id} has no target pattern.");

            var contentNode = row.GetProperty("content");
            var rule = new SupportingPracticeTargetRule(
                id,
                titlePatterns,
                codePatterns,
                Required(row, "skillId"),
                Required(row, "mechanic"),
                ReadList(row, "families"),
                new SupportingPracticeContentRecipe(
                    Required(contentNode, "concept"),
                    Required(contentNode, "workedExample"),
                    Required(contentNode, "solution"),
                    Required(contentNode, "commonMistake"),
                    Required(contentNode, "summary")));

            if (rule.Families.Count == 0)
                throw new InvalidOperationException($"Supporting Practice rule {id} has no question family.");

            result.Add(new CompiledRule(
                rule,
                Compile(titlePatterns, id, "titlePatterns"),
                Compile(codePatterns, id, "codePatterns")));
        }

        return result;
    }

    private static IReadOnlyList<Regex> Compile(
        IReadOnlyList<string> patterns,
        string ruleId,
        string field) =>
        patterns.Select(pattern =>
        {
            try
            {
                return new Regex(
                    pattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant |
                    RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Invalid regex in Supporting Practice rule {ruleId}.{field}: {pattern}",
                    ex);
            }
        }).ToArray();

    private static IReadOnlyList<string> ReadList(JsonElement row, string name) =>
        row.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Array
            ? node.EnumerateArray()
                .Select(value => value.GetString()?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray()
            : [];

    private static string Required(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var node))
            throw new InvalidOperationException($"Supporting Practice rule field is missing: {name}.");
        var value = node.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Supporting Practice rule field is blank: {name}.");
        return value;
    }

    private sealed record CompiledRule(
        SupportingPracticeTargetRule Rule,
        IReadOnlyList<Regex> TitlePatterns,
        IReadOnlyList<Regex> CodePatterns);
}
