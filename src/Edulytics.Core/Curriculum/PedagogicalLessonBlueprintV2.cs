using System.Text.RegularExpressions;

namespace Edulytics.Core.Curriculum;

public sealed class PedagogicalLessonBlueprintSource
{
    public string SourceKey { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string RootUrl { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;

    public string RequiredDigitalAttribution
    { get; set; } = string.Empty;

    public string RightsNote { get; set; } = string.Empty;

    public List<string> EvidenceUrls { get; set; } =
        [];
}

public sealed class PedagogicalLessonBlueprintFormalTarget
{
    public string OutcomeCode { get; set; } = string.Empty;
    public string EvidenceKind { get; set; } = string.Empty;
    public bool PublisherSuppliedAlignment { get; set; }
    public int SortOrder { get; set; }

    public List<PedagogicalLessonBlueprintEvidenceReference>
        EvidenceReferences
    { get; set; } =
        [];
}

public sealed class PedagogicalLessonBlueprintEvidenceReference
{
    public string? SourceKey { get; set; }
    public string SourceFamily { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SectionOrTask { get; set; } = string.Empty;
    public string? EvidenceSha256 { get; set; }
    public string? License { get; set; }
}

public static class PedagogicalLessonBlueprintV2Contract
{
    private static readonly HashSet<string>
        SequenceAuthorities =
        new(StringComparer.Ordinal)
        {
            "PublisherSourceSequence",
            "EdulyticsOwnedSequence"
        };

    private static readonly HashSet<string>
        EvidenceKinds =
        new(StringComparer.Ordinal)
        {
            "PublisherAddressing",
            "PrimarySourceExplicitStandardAlignment",
            "VerifiedContentCoverage"
        };

    private static readonly HashSet<string> Roles =
        new(StringComparer.Ordinal)
        {
            "BuildingOn",
            "Addressing",
            "BuildingTowards"
        };

    private static readonly HashSet<string>
        ReferenceKinds =
        new(StringComparer.Ordinal)
        {
            "Domain",
            "Cluster",
            "NumberedStandard",
            "StandardSubpart"
        };

    private static readonly HashSet<string>
        ResolutionKinds =
        new(StringComparer.Ordinal)
        {
            "None",
            "ExactAcceptedStandard",
            "SubpartToAcceptedParent"
        };

    private static readonly Regex ShaPattern =
        new(
            "^[0-9a-f]{64}$",
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    private static readonly Regex CoursePattern =
        new(
            "^[A-Z0-9][A-Z0-9-]{1,63}$",
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    public static void Validate(
        PedagogicalLessonBlueprintDocument document)
    {
        MathematicsCurriculumPackRegistry.Validate();

        if (document.SchemaVersion != 2)
            throw new InvalidOperationException(
                "Schema V2 validator requires SchemaVersion 2.");

        Require(document.BlueprintCode, "BlueprintCode");
        Require(document.PackCode, "PackCode");
        Require(document.VersionCode, "VersionCode");
        Require(document.CourseCode, "CourseCode");
        Require(document.NativeLevel, "NativeLevel");
        Require(document.SequenceAuthority, "SequenceAuthority");
        Require(document.OfficialAuthority, "OfficialAuthority");

        RequireHttp(
            document.OfficialSourceUrl,
            "OfficialSourceUrl");

        Require(
            document.SourceSelectionReason,
            "SourceSelectionReason");

        Require(
            document.SourceSelectionEvidence,
            "SourceSelectionEvidence");

        RequireSha(
            document.SemanticGraphSha256,
            "SemanticGraphSha256");

        if (document.LogicalLevel != 0)
            throw new InvalidOperationException(
                "Schema V2 must not use legacy LogicalLevel.");

        if (document.LogicalLevelFrom <= 0 ||
            document.LogicalLevelTo <= 0 ||
            document.LogicalLevelFrom >
                document.LogicalLevelTo ||
            document.LogicalLevelTo > 13)
        {
            throw new InvalidOperationException(
                "Schema V2 logical range is invalid.");
        }

        if (!CoursePattern.IsMatch(
                document.CourseCode))
        {
            throw new InvalidOperationException(
                "Schema V2 CourseCode is invalid.");
        }

        if (!SequenceAuthorities.Contains(
                document.SequenceAuthority))
        {
            throw new InvalidOperationException(
                "Unsupported Schema V2 SequenceAuthority.");
        }

        if (!document
                .SuppressOutcomeFallbackForLogicalRange)
        {
            throw new InvalidOperationException(
                "Schema V2 must explicitly suppress " +
                "fallback for its logical range.");
        }

        if (!DateTimeOffset.TryParse(
                document.SourceCheckedAtUtc,
                out var checkedAt) ||
            checkedAt.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "SourceCheckedAtUtc must be valid UTC.");
        }

        if (!MathematicsCurriculumPackRegistry.All.Any(
                x =>
                    string.Equals(
                        x.Code,
                        document.PackCode,
                        StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Unsupported Mathematics blueprint pack: " +
                $"{document.PackCode}.");
        }

        if (document.Sources.Count == 0)
            throw new InvalidOperationException(
                "Schema V2 requires at least one source.");

        var sourceKeys =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var source in document.Sources)
        {
            Require(source.SourceKey, "SourceKey");
            Require(source.SourceType, "SourceType");
            Require(source.Title, "SourceTitle");
            Require(source.Publisher, "SourcePublisher");
            Require(source.Edition, "SourceEdition");
            RequireHttp(source.RootUrl, "SourceRootUrl");
            Require(source.License, "SourceLicense");

            PedagogicalSourceLicensePolicy.Validate(
                source.License);

            Require(
                source.RequiredDigitalAttribution,
                "RequiredDigitalAttribution");

            Require(
                source.RightsNote,
                "SourceRightsNote");

            if (!sourceKeys.Add(
                    source.SourceKey))
            {
                throw new InvalidOperationException(
                    $"Duplicate Schema V2 SourceKey: " +
                    $"{source.SourceKey}.");
            }

            foreach (var url in source.EvidenceUrls)
                RequireHttp(
                    url,
                    "SourceEvidenceUrls");
        }

        if (document.Units.Count == 0 ||
            document.Lessons.Count == 0)
        {
            throw new InvalidOperationException(
                "Schema V2 requires units and lessons.");
        }

        var unitsByNumber =
            document.Units.ToDictionary(
                x => x.Number);

        if (unitsByNumber.Count !=
            document.Units.Count)
        {
            throw new InvalidOperationException(
                "Duplicate Schema V2 unit number.");
        }

        var unitCodes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var unitSorts =
            new HashSet<int>();

        foreach (var unit in document.Units)
        {
            if (unit.Number <= 0 ||
                unit.LessonCount <= 0 ||
                unit.SortOrder <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid Schema V2 unit: " +
                    $"{unit.Number}.");
            }

            Require(unit.UnitCode, "UnitCode");
            Require(unit.Title, "UnitTitle");
            RequireHttp(unit.SourceUrl, "UnitSourceUrl");
            RequireSha(unit.SemanticSha256, "UnitSemanticSha256");

            if (!unitCodes.Add(unit.UnitCode) ||
                !unitSorts.Add(unit.SortOrder))
            {
                throw new InvalidOperationException(
                    "Duplicate V2 UnitCode/SortOrder.");
            }
        }

        var lessonCodes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var sourceCodes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var lessonSorts =
            new HashSet<int>();

        var unitLessonNumbers =
            new HashSet<(int, int)>();

        foreach (var lesson in document.Lessons)
        {
            Require(
                lesson.SourceLessonCode,
                "SourceLessonCode");

            Require(
                lesson.LessonCode,
                "LessonCode");

            Require(
                lesson.UnitTitle,
                "LessonUnitTitle");

            Require(
                lesson.Title,
                "LessonTitle");

            RequireHttp(
                lesson.SourceUrl,
                "LessonSourceUrl");

            RequireSha(
                lesson.SemanticSha256,
                "LessonSemanticSha256");

            if (!lesson.LessonCode.StartsWith(
                    "PED:",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Schema V2 lesson requires PED identity.");
            }

            if (!lessonCodes.Add(
                    lesson.LessonCode) ||
                !sourceCodes.Add(
                    lesson.SourceLessonCode))
            {
                throw new InvalidOperationException(
                    "Duplicate Schema V2 lesson identity.");
            }

            if (lesson.SortOrder <= 0 ||
                !lessonSorts.Add(
                    lesson.SortOrder))
            {
                throw new InvalidOperationException(
                    "Invalid/duplicate V2 lesson SortOrder.");
            }

            if (!unitsByNumber.TryGetValue(
                    lesson.UnitNumber,
                    out var unit) ||
                !string.Equals(
                    unit.Title,
                    lesson.UnitTitle,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Schema V2 lesson/unit drift.");
            }

            if (lesson.LessonNumber <= 0 ||
                !unitLessonNumbers.Add(
                    (
                        lesson.UnitNumber,
                        lesson.LessonNumber
                    )))
            {
                throw new InvalidOperationException(
                    "Invalid/duplicate V2 LessonNumber.");
            }

            if (lesson.OutcomeCodes.Count != 0)
            {
                throw new InvalidOperationException(
                    "Schema V2 uses FormalTargets; " +
                    "OutcomeCodes must be empty.");
            }

            if (lesson.ApplicableCourses.Any(
                    string.IsNullOrWhiteSpace) ||
                lesson.ApplicableCourses.Count !=
                    lesson.ApplicableCourses
                        .Distinct(
                            StringComparer.Ordinal)
                        .Count())
            {
                throw new InvalidOperationException(
                    "Invalid V2 ApplicableCourses.");
            }

            ValidateAlignments(
                lesson);

            ValidateTargets(
                lesson,
                sourceKeys);
        }

        foreach (var unit in document.Units)
        {
            var actual =
                document.Lessons.Count(
                    x =>
                        x.UnitNumber ==
                        unit.Number);

            if (actual != unit.LessonCount)
            {
                throw new InvalidOperationException(
                    $"Unit lesson-count drift: " +
                    $"{unit.UnitCode}.");
            }
        }

        var targets =
            document.Lessons
                .SelectMany(
                    x => x.FormalTargets)
                .ToArray();

        var distinctOutcomes =
            targets
                .Select(
                    x => x.OutcomeCode)
                .Distinct(
                    StringComparer.Ordinal)
                .Count();

        var diagnostics =
            document.AcquisitionDiagnostics;

        if (diagnostics.UnitCount !=
                document.Units.Count ||
            diagnostics.LessonCount !=
                document.Lessons.Count ||
            diagnostics.EffectiveOfficialStandardCount !=
                distinctOutcomes ||
            diagnostics.EffectiveOfficialStandardCount <= 0 ||
            diagnostics.FormalMappingCount !=
                targets.Length)
        {
            throw new InvalidOperationException(
                "Schema V2 diagnostics drift.");
        }
    }

    private static void ValidateAlignments(
        PedagogicalLessonBlueprintLesson lesson)
    {
        var keys =
            new HashSet<(string, string)>();

        var sorts =
            new HashSet<int>();

        foreach (var alignment in lesson.Alignments)
        {
            Require(alignment.Role, "Role");
            Require(alignment.ReferenceCode, "ReferenceCode");
            Require(alignment.ReferenceKind, "ReferenceKind");
            Require(alignment.ResolutionKind, "ResolutionKind");

            if (!Roles.Contains(alignment.Role) ||
                !ReferenceKinds.Contains(
                    alignment.ReferenceKind) ||
                !ResolutionKinds.Contains(
                    alignment.ResolutionKind))
            {
                throw new InvalidOperationException(
                    "Unsupported Schema V2 alignment.");
            }

            if (!keys.Add(
                    (
                        alignment.Role,
                        alignment.ReferenceCode
                    )) ||
                alignment.SortOrder <= 0 ||
                !sorts.Add(
                    alignment.SortOrder))
            {
                throw new InvalidOperationException(
                    "Duplicate Schema V2 alignment.");
            }

            var resolved =
                alignment.ResolutionKind is
                    "ExactAcceptedStandard" or
                    "SubpartToAcceptedParent";

            if (resolved)
            {
                if (alignment.Role != "Addressing" ||
                    string.IsNullOrWhiteSpace(
                        alignment.OutcomeCode) ||
                    !lesson.FormalTargets.Any(
                        x =>
                            x.EvidenceKind ==
                                "PublisherAddressing" &&
                            x.OutcomeCode ==
                                alignment.OutcomeCode))
                {
                    throw new InvalidOperationException(
                        "Resolved V2 alignment lacks " +
                        "PublisherAddressing target.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(
                         alignment.OutcomeCode))
            {
                throw new InvalidOperationException(
                    "Unresolved alignment cannot carry OutcomeCode.");
            }

            if ((alignment.ReferenceKind is
                    "Cluster" or "Domain") &&
                !string.IsNullOrWhiteSpace(
                    alignment.OutcomeCode))
            {
                throw new InvalidOperationException(
                    "Cluster/domain formal expansion forbidden.");
            }
        }
    }

    private static void ValidateTargets(
        PedagogicalLessonBlueprintLesson lesson,
        IReadOnlySet<string> sourceKeys)
    {
        var outcomes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var sorts =
            new HashSet<int>();

        foreach (var target in lesson.FormalTargets)
        {
            Require(
                target.OutcomeCode,
                "FormalTargetOutcomeCode");

            Require(
                target.EvidenceKind,
                "EvidenceKind");

            if (!outcomes.Add(
                    target.OutcomeCode) ||
                target.SortOrder <= 0 ||
                !sorts.Add(
                    target.SortOrder))
            {
                throw new InvalidOperationException(
                    "Duplicate/invalid V2 FormalTarget.");
            }

            if (!EvidenceKinds.Contains(
                    target.EvidenceKind))
            {
                throw new InvalidOperationException(
                    "Unsupported V2 EvidenceKind.");
            }

            switch (target.EvidenceKind)
            {
                case "PublisherAddressing":
                    if (!target.PublisherSuppliedAlignment ||
                        !lesson.Alignments.Any(
                            x =>
                                x.Role == "Addressing" &&
                                x.OutcomeCode ==
                                    target.OutcomeCode &&
                                x.ResolutionKind is
                                    "ExactAcceptedStandard" or
                                    "SubpartToAcceptedParent"))
                    {
                        throw new InvalidOperationException(
                            "PublisherAddressing provenance missing.");
                    }

                    break;

                case "PrimarySourceExplicitStandardAlignment":
                    if (!target.PublisherSuppliedAlignment ||
                        target.EvidenceReferences.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Primary explicit evidence missing.");
                    }

                    break;

                case "VerifiedContentCoverage":
                    if (target.PublisherSuppliedAlignment ||
                        target.EvidenceReferences.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "VerifiedContentCoverage provenance invalid.");
                    }

                    break;
            }

            foreach (var evidence in
                     target.EvidenceReferences)
            {
                Require(
                    evidence.SourceFamily,
                    "EvidenceSourceFamily");

                RequireHttp(
                    evidence.SourceUrl,
                    "EvidenceSourceUrl");

                Require(
                    evidence.SectionOrTask,
                    "EvidenceSectionOrTask");

                if (!string.IsNullOrWhiteSpace(
                        evidence.SourceKey) &&
                    !sourceKeys.Contains(
                        evidence.SourceKey))
                {
                    throw new InvalidOperationException(
                        "Unknown V2 evidence SourceKey.");
                }

                if (!string.IsNullOrWhiteSpace(
                        evidence.EvidenceSha256))
                {
                    RequireSha(
                        evidence.EvidenceSha256,
                        "EvidenceSha256");
                }

                if (!string.IsNullOrWhiteSpace(
                        evidence.License))
                {
                    PedagogicalSourceLicensePolicy.Validate(
                        evidence.License);
                }
            }
        }
    }

    private static void Require(
        string? value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Schema V2 field required: {field}.");
        }
    }

    private static void RequireHttp(
        string? value,
        string field)
    {
        Require(value, field);

        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Schema V2 {field} must be HTTP/HTTPS.");
        }
    }

    private static void RequireSha(
        string? value,
        string field)
    {
        Require(value, field);

        if (!ShaPattern.IsMatch(value!))
        {
            throw new InvalidOperationException(
                $"Schema V2 {field} must be lowercase SHA-256.");
        }
    }
}
