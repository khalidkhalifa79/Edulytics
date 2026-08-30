using System.Net;
using System.Text.RegularExpressions;

namespace Edulytics.Web.Presentation;

public enum LessonVisualType
{
    MeasuredPath,
    DoubleNumberLine,
    NumberLine,
    CoordinatePlane,
    AreaDecomposition,
    ArrayOrGrid,
    FractionOrRatioBar,
    GeometricFigure
}

public sealed record LessonVisualSpec(
    LessonVisualType Type,
    string AccessibilityText,
    string Title,
    string PrimaryLabel,
    string SecondaryLabel,
    IReadOnlyList<string> PrimaryValues,
    IReadOnlyList<string> SecondaryValues,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> Measures,
    string Variant)
{
    public string Caption =>
        Type switch
        {
            LessonVisualType.MeasuredPath
                when Labels.Count >= 3 =>
                $"{Labels[0]} → {Labels[1]} → {Labels[2]}",

            LessonVisualType.DoubleNumberLine
                when !string.IsNullOrWhiteSpace(PrimaryLabel)
                     && !string.IsNullOrWhiteSpace(SecondaryLabel) =>
                $"{PrimaryLabel} and {SecondaryLabel}",

            LessonVisualType.NumberLine =>
                "Number line representation",

            LessonVisualType.CoordinatePlane =>
                "Coordinate-plane representation",

            LessonVisualType.AreaDecomposition
                when Variant == "area-four-panels" =>
                "Which diagrams use equal-sized square units correctly?",

            LessonVisualType.AreaDecomposition
                when Variant == "area-tangram" =>
                "Decompose and rearrange the pieces without changing total area.",

            LessonVisualType.AreaDecomposition =>
                "Same total area — a different decomposition.",

            LessonVisualType.ArrayOrGrid =>
                "Array and square-unit representation",

            LessonVisualType.FractionOrRatioBar =>
                "Segmented bar representation",

            LessonVisualType.GeometricFigure =>
                "Geometric representation",

            _ =>
                "Instructional diagram"
        };
}

public sealed record LessonPresentationItem(
    string? Text,
    LessonVisualSpec? Visual)
{
    public bool IsVisual => Visual is not null;

    public LessonVisualType? VisualType =>
        Visual?.Type;

    public string? AccessibilityText =>
        Visual?.AccessibilityText;
}

public static class LessonPresentationParser
{
    private static readonly Regex DangerousElementRegex =
        new(
            @"<(script|style)\b[^>]*>[\s\S]*?</\1\s*>",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex StructuralTagRegex =
        new(
            @"</?(?:p|br|div|li|ul|ol|section|article|h[1-6])\b[^>]*>",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex ResidualTagRegex =
        new(
            @"<[^>]*>",
            RegexOptions.CultureInvariant);

    private static readonly Regex BlockBoundaryRegex =
        new(
            @"(?:\n\s*\n)+|" +
            @"(?=(?:Step|Krok|Example|Przykład|Mistake|Błąd)" +
            @"\s+\d+\s*[:.])",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex StepPrefixRegex =
        new(
            @"^\s*(?:Step|Krok)\s+\d+\s*[:.]\s*",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex PresentationNoiseLineRegex =
        new(
            @"^\s*(?:" +
            @"Supports accessibility for\s*:.*|" +
            @"Design Principle\(s\)\s*:.*|" +
            @"Source reasoning\s*/\s*synthesis\s*:?\s*|" +
            @"Source reasoning\s*:\s*|" +
            @"Reasoning\s*:\s*|" +
            @"Digital\s*|" +
            @"Activity\s*|" +
            @"Student Facing\s*" +
            @")$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex TeacherDirectionLineRegex =
        new(
            @"^\s*(?:" +
            @"Before class\b|" +
            @"Give each\b|" +
            @"Give students\b|" +
            @"Invite\b|" +
            @"Select\b|" +
            @"As students\b|" +
            @"Monitor\b|" +
            @"Tell students\b|" +
            @"Ask students\b|" +
            @"Prompt students\b|" +
            @"Have students\b|" +
            @"Consider\b|" +
            @"Display\b|" +
            @"Record and display\b|" +
            @"Create a display\b|" +
            @"Close this conversation\b|" +
            @"Circulate\b|" +
            @"Arrange students\b|" +
            @"If pairs\b|" +
            @"Classrooms using\b|" +
            @"Use a physical\b|" +
            @"If using the applet\b" +
            @")",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex TeacherMaterialLineRegex =
        new(
            @"^\s*(?:" +
            @"Square\s*:\s*\d+\s*|" +
            @"Small triangles?\s*:\s*\d+\s*|" +
            @"Medium triangles?\s*:\s*\d+\s*|" +
            @"Large triangles?\s*:\s*\d+\s*" +
            @")$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex ExampleScaffoldRegex =
        new(
            @"^(?<prefix>\s*Example\s+\d+\s*:)\s*" +
            @"(?:Student Facing|Activity)\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex SentenceBoundaryRegex =
        new(
            @"(?<=[.!?])\s+(?=[A-Z0-9“""])",
            RegexOptions.CultureInvariant);

    private static readonly Regex DescriptionRegex =
        new(
            @"\bDescription\s*:\s*" +
            @"(?<description>.*?)" +
            @"(?=" +
                @"(?:\n\s*\n)" +
                @"|" +
                @"(?:\n\s*(?:" +
                    @"Source reasoning|" +
                    @"Reasoning|" +
                    @"Example\s+\d+|" +
                    @"Step\s+\d+|" +
                    @"Moving\s+slowly|" +
                    @"Moving\s+quickly|" +
                    @"Estimate|" +
                    @"The person|" +
                    @"On the |" +
                    @"Repeat |" +
                    @"Trade diagrams|" +
                    @"Write |" +
                    @"Use |" +
                    @"Your teacher" +
                @")\b)" +
                @"|" +
                @"\z" +
            @")",
            RegexOptions.IgnoreCase |
            RegexOptions.Singleline |
            RegexOptions.CultureInvariant);

    public static IReadOnlyList<LessonPresentationItem> Parse(
        string? value,
        bool orderedSteps = false,
        string sectionKind = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var safe =
            ToPlainStructure(value);

        safe =
            PrepareLearnerFacingText(
                safe,
                sectionKind);

        var result =
            new List<LessonPresentationItem>();

        foreach (var rawBlock in
            SplitDisplayBlocks(
                safe,
                sectionKind))
        {
            var block =
                NormalizeText(rawBlock);

            if (orderedSteps)
            {
                block =
                    RemoveOrderedStepPrefix(block);
            }

            if (string.IsNullOrWhiteSpace(block))
            {
                continue;
            }

            AppendBlock(
                result,
                block);
        }

        return result;
    }

    private static string PrepareLearnerFacingText(
        string value,
        string sectionKind)
    {
        value =
            Regex.Replace(
                value,
                @"\bDescription\s*:\s*\n+\s*",
                "Description: ",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        var learnerSections =
            sectionKind is
                "explanation" or
                "concepts" or
                "examples" or
                "steps" or
                "summary";

        var lines =
            value.Split('\n');

        var result =
            new List<string>();

        foreach (var sourceLine in lines)
        {
            var line =
                sourceLine.Trim();

            if (line.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            if (PresentationNoiseLineRegex.IsMatch(line))
            {
                continue;
            }

            var example =
                ExampleScaffoldRegex.Match(line);

            if (example.Success)
            {
                result.Add(
                    example.Groups["prefix"]
                        .Value
                        .Trim());

                continue;
            }

            if (
                learnerSections &&
                TeacherDirectionLineRegex.IsMatch(line))
            {
                continue;
            }

            if (
                sectionKind == "explanation" &&
                TeacherMaterialLineRegex.IsMatch(line))
            {
                continue;
            }

            if (
                sectionKind == "summary" &&
                line.EndsWith(
                    "?",
                    StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(line);
        }

        return string.Join(
            '\n',
            result);
    }

    private static IEnumerable<string> SplitDisplayBlocks(
        string value,
        string sectionKind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        if (sectionKind == "summary")
        {
            var compact =
                NormalizeText(
                    value.Replace(
                        '\n',
                        ' '));

            return SentenceBoundaryRegex
                .Split(compact)
                .Select(x => x.Trim())
                .Where(x => x.Length >= 12)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        }

        if (
            sectionKind is
                "explanation" or
                "concepts" or
                "examples" or
                "mistakes")
        {
            return value
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        return BlockBoundaryRegex.Split(value);
    }

    public static string ToPlainText(
        string? value)
    {
        return NormalizeText(
            ToPlainStructure(
                value ?? string.Empty));
    }

    public static string RemoveOrderedStepPrefix(
        string value)
    {
        return StepPrefixRegex
            .Replace(
                value,
                string.Empty)
            .Trim();
    }

    private static void AppendBlock(
        List<LessonPresentationItem> result,
        string block)
    {
        var matches =
            DescriptionRegex.Matches(block);

        if (matches.Count == 0)
        {
            result.Add(
                new(
                    block,
                    null));

            return;
        }

        var cursor = 0;

        foreach (Match match in matches)
        {
            var before =
                NormalizeText(
                    block[cursor..match.Index]);

            if (!string.IsNullOrWhiteSpace(before))
            {
                result.Add(
                    new(
                        before,
                        null));
            }

            var description =
                NormalizeText(
                    match.Groups["description"].Value);

            if (!string.IsNullOrWhiteSpace(description))
            {
                var visual =
                    TryCreateVisual(
                        description);

                if (visual is not null)
                {
                    result.Add(
                        new(
                            null,
                            visual));
                }
                else
                {
                    /*
                     * Fail-safe behaviour:
                     * keep useful description as clean text.
                     * Never fabricate a diagram.
                     */
                    result.Add(
                        new(
                            description,
                            null));
                }
            }

            cursor =
                match.Index +
                match.Length;
        }

        if (cursor < block.Length)
        {
            var after =
                NormalizeText(
                    block[cursor..]);

            if (!string.IsNullOrWhiteSpace(after))
            {
                result.Add(
                    new(
                        after,
                        null));
            }
        }
    }

    private static LessonVisualSpec? TryCreateVisual(
        string description)
    {
        var lower =
            description.ToLowerInvariant();

        /*
         * -----------------------------------------------------
         * Measured path
         * -----------------------------------------------------
         *
         * Only render when the description explicitly identifies
         * three marks and their relationships.
         */
        if (
            lower.Contains(
                "line with three markings") &&
            lower.Contains(
                "first mark") &&
            lower.Contains(
                "second mark") &&
            lower.Contains(
                "third mark"))
        {
            var labels =
                new[]
                {
                    ExtractQuotedLabel(
                        description,
                        "first"),
                    ExtractQuotedLabel(
                        description,
                        "second"),
                    ExtractQuotedLabel(
                        description,
                        "third")
                };

            var measure1 =
                ExtractMeasure(
                    description,
                    "first",
                    "second");

            var measure2 =
                ExtractMeasure(
                    description,
                    "second",
                    "third");

            if (
                labels.All(
                    x => !string.IsNullOrWhiteSpace(x)) &&
                !string.IsNullOrWhiteSpace(measure1) &&
                !string.IsNullOrWhiteSpace(measure2))
            {
                return new(
                    LessonVisualType.MeasuredPath,
                    description,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    [],
                    [],
                    labels!,
                    [
                        measure1,
                        measure2
                    ],
                    "three-mark-path");
            }
        }

        /*
         * -----------------------------------------------------
         * Double number line
         * -----------------------------------------------------
         *
         * Values and labels are extracted from the actual source
         * description. Nothing is invented.
         */
        if (
            lower.Contains(
                "double number line"))
        {
            var topSection =
                ExtractAxisSection(
                    description,
                    "top",
                    "bottom");

            var bottomSection =
                ExtractAxisSection(
                    description,
                    "bottom",
                    null);

            if (
                topSection is not null &&
                bottomSection is not null)
            {
                var topLabel =
                    ExtractAxisLabel(
                        topSection);

                var bottomLabel =
                    ExtractAxisLabel(
                        bottomSection);

                var topValues =
                    ExtractAxisValues(
                        topSection);

                var bottomValues =
                    ExtractAxisValues(
                        bottomSection);

                if (
                    topValues.Count >= 2 &&
                    bottomValues.Count >= 2)
                {
                    var title =
                        ExtractDiagramTitle(
                            description);

                    return new(
                        LessonVisualType.DoubleNumberLine,
                        description,
                        title,
                        topLabel,
                        bottomLabel,
                        topValues,
                        bottomValues,
                        [],
                        [],
                        "double-number-line");
                }
            }
        }

        /*
         * -----------------------------------------------------
         * Single number line
         * -----------------------------------------------------
         */
        if (
            lower.Contains(
                "number line") &&
            !lower.Contains(
                "double number line"))
        {
            var values =
                ExtractAxisValues(
                    description);

            if (values.Count >= 2)
            {
                return new(
                    LessonVisualType.NumberLine,
                    description,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    values,
                    [],
                    [],
                    [],
                    "number-line");
            }
        }

        /*
         * -----------------------------------------------------
         * Coordinate plane
         * -----------------------------------------------------
         */
        if (
            lower.Contains(
                "coordinate plane") &&
            (
                lower.Contains(
                    "horizontal axis") ||
                lower.Contains(
                    "vertical axis") ||
                lower.Contains(
                    "x-axis") ||
                lower.Contains(
                    "y-axis")))
        {
            return new(
                LessonVisualType.CoordinatePlane,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "coordinate-plane");
        }

        /*
         * -----------------------------------------------------
         * Area / decomposition visuals
         * -----------------------------------------------------
         */
        if (
            lower.Contains(
                "four drawings") &&
            lower.Contains(
                "shape a") &&
            lower.Contains(
                "shape b") &&
            lower.Contains(
                "shape c") &&
            lower.Contains(
                "shape d") &&
            lower.Contains(
                "squares"))
        {
            return new(
                LessonVisualType.AreaDecomposition,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "area-four-panels");
        }

        if (
            lower.Contains(
                "tangram"))
        {
            return new(
                LessonVisualType.AreaDecomposition,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "area-tangram");
        }

        if (
            (
                lower.Contains(
                    "decompos") ||
                lower.Contains(
                    "rearrang")
            ) &&
            lower.Contains(
                "area"))
        {
            return new(
                LessonVisualType.AreaDecomposition,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "area-rearrangement");
        }

        /*
         * -----------------------------------------------------
         * Array / grid
         * -----------------------------------------------------
         */
        if (
            lower.Contains(
                "array") ||
            lower.Contains(
                "grid of squares"))
        {
            return new(
                LessonVisualType.ArrayOrGrid,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "array-grid");
        }

        /*
         * -----------------------------------------------------
         * Fraction / ratio bar
         * -----------------------------------------------------
         */
        if (
            lower.Contains(
                "fraction bar") ||
            lower.Contains(
                "ratio bar") ||
            lower.Contains(
                "tape diagram"))
        {
            return new(
                LessonVisualType.FractionOrRatioBar,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                "segmented-bar");
        }

        /*
         * -----------------------------------------------------
         * Geometry
         * -----------------------------------------------------
         */
        var geometryVariant =
            lower.Contains("triangle")
                ? "triangle"
                : lower.Contains("rectangle")
                    ? "rectangle"
                    : lower.Contains("quadrilateral")
                        ? "quadrilateral"
                        : lower.Contains("polygon")
                            ? "polygon"
                            : string.Empty;

        if (
            !string.IsNullOrWhiteSpace(
                geometryVariant))
        {
            return new(
                LessonVisualType.GeometricFigure,
                description,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                [],
                [],
                geometryVariant);
        }

        /*
         * Unknown visual description:
         * intentionally no generic fake drawing.
         */
        return null;
    }

    private static string ExtractQuotedLabel(
        string description,
        string ordinal)
    {
        var match =
            Regex.Match(
                description,
                $@"the\s+{Regex.Escape(ordinal)}\s+mark\s+is\s+labeled\s+[""“](?<label>[^""”]+)[""”]",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        return match.Success
            ? match.Groups["label"].Value.Trim()
            : string.Empty;
    }

    private static string ExtractMeasure(
        string description,
        string first,
        string second)
    {
        var match =
            Regex.Match(
                description,
                $@"distance\s+between\s+the\s+{Regex.Escape(first)}\s+and\s+{Regex.Escape(second)}\s+mark\s+is\s+labeled\s+(?<value>[^.]+)",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        return match.Success
            ? match.Groups["value"].Value.Trim()
            : string.Empty;
    }

    private static string? ExtractAxisSection(
        string description,
        string axis,
        string? stopAxis)
    {
        var stop =
            stopAxis is null
                ? @"\z"
                : $@"(?=(?:the\s+)?{Regex.Escape(stopAxis)}(?:\s+number)?\s+line\b)";

        var match =
            Regex.Match(
                description,
                $@"(?:the\s+)?{Regex.Escape(axis)}(?:\s+number)?\s+line(?<body>.*?){stop}",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline |
                RegexOptions.CultureInvariant);

        return match.Success
            ? match.Groups["body"].Value.Trim()
            : null;
    }

    private static string ExtractAxisLabel(
        string section)
    {
        var quoted =
            Regex.Match(
                section,
                @"(?:is\s+)?labeled\s+[""“](?<label>[^""”]+)[""”]",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        if (quoted.Success)
        {
            return quoted.Groups["label"].Value.Trim();
        }

        var comma =
            Regex.Match(
                section,
                @"^\s*,\s*(?<label>[^.]+)\.",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        if (comma.Success)
        {
            return comma.Groups["label"].Value.Trim();
        }

        return string.Empty;
    }

    private static IReadOnlyList<string> ExtractAxisValues(
        string section)
    {
        var match =
            Regex.Match(
                section,
                @"numbers\s+(?<values>.*?)\s+are\s+indicated",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline |
                RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            match =
                Regex.Match(
                    section,
                    @"labels\s*:\s*(?<values>[^.]+)",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline |
                    RegexOptions.CultureInvariant);
        }

        if (!match.Success)
        {
            return [];
        }

        var raw =
            match.Groups["values"]
                .Value
                .Replace(
                    " and ",
                    ", ",
                    StringComparison.OrdinalIgnoreCase);

        var values =
            raw
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(
                    CleanAxisValue)
                .Where(
                    x => x is not null)
                .Select(
                    x => x!)
                .ToList();

        if (
            section.Contains(
                "last tick mark is blank",
                StringComparison.OrdinalIgnoreCase) &&
            (
                values.Count == 0 ||
                values[^1] != string.Empty
            ))
        {
            values.Add(
                string.Empty);
        }

        return values;
    }

    private static string? CleanAxisValue(
        string raw)
    {
        var value =
            raw.Trim()
                .Trim(
                    '.',
                    ';',
                    ':');

        if (
            value.Equals(
                "blank",
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (
            value.Equals(
                "?",
                StringComparison.Ordinal))
        {
            return value;
        }

        if (
            Regex.IsMatch(
                value,
                @"^-?\d+(?:\.\d+)?(?:/\d+)?$",
                RegexOptions.CultureInvariant))
        {
            return value;
        }

        return null;
    }

    private static string ExtractDiagramTitle(
        string description)
    {
        var match =
            Regex.Match(
                description,
                @"double\s+number\s+line\s+titled\s*,?\s*(?<title>[^.]+)",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        return match.Success
            ? match.Groups["title"].Value.Trim()
            : string.Empty;
    }

    private static string ToPlainStructure(
        string value)
    {
        /*
         * Decode twice at most:
         * handles encoded source markup such as &lt;p&gt; safely.
         */
        var decoded =
            WebUtility.HtmlDecode(value);

        decoded =
            WebUtility.HtmlDecode(decoded);

        decoded =
            DangerousElementRegex.Replace(
                decoded,
                string.Empty);

        decoded =
            StructuralTagRegex.Replace(
                decoded,
                "\n");

        decoded =
            ResidualTagRegex.Replace(
                decoded,
                string.Empty);

        return decoded
            .Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal)
            .Replace(
                '\r',
                '\n');
    }

    private static string NormalizeText(
        string value)
    {
        var result =
            Regex.Replace(
                value,
                @"[ \t]+",
                " ",
                RegexOptions.CultureInvariant);

        result =
            Regex.Replace(
                result,
                @" *\n *",
                "\n",
                RegexOptions.CultureInvariant);

        result =
            Regex.Replace(
                result,
                @"\n{3,}",
                "\n\n",
                RegexOptions.CultureInvariant);

        return result.Trim();
    }
}
