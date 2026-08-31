using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Edulytics.Core.Curriculum;

/// <summary>
/// Edulytics-owned pedagogical lesson sequence sourced from traceable
/// instructional material. A blueprint never creates official curriculum
/// authority; the accepted official Standards/Outcomes remain authoritative.
/// </summary>
public sealed class PedagogicalLessonBlueprintDocument
{
    public int SchemaVersion { get; set; }

    public string BlueprintCode { get; set; } =
        string.Empty;

    public string PackCode { get; set; } =
        string.Empty;

    public string VersionCode { get; set; } =
        string.Empty;

    public int LogicalLevel { get; set; }

    // Schema V2 course scope. Schema V1 continues to use
    // LogicalLevel exactly as before.
    public string CourseCode { get; set; } =
        string.Empty;

    public int LogicalLevelFrom { get; set; }

    public int LogicalLevelTo { get; set; }

    public string SequenceAuthority { get; set; } =
        string.Empty;

    public bool SuppressOutcomeFallbackForLogicalRange
    { get; set; }

    public List<PedagogicalLessonBlueprintSource>
        Sources
    { get; set; } =
        [];

    public string NativeLevel { get; set; } =
        string.Empty;

    public string? Pathway { get; set; }

    public string OfficialAuthority { get; set; } =
        string.Empty;

    public string OfficialSourceUrl { get; set; } =
        string.Empty;

    public string PedagogicalSourceType { get; set; } =
        string.Empty;

    public string SourceTitle { get; set; } =
        string.Empty;

    public string SourcePublisher { get; set; } =
        string.Empty;

    public string SourceEdition { get; set; } =
        string.Empty;

    public string SourceRootUrl { get; set; } =
        string.Empty;

    public string SourceCheckedAtUtc { get; set; } =
        string.Empty;

    public string SourceLicense { get; set; } =
        string.Empty;

    public string RequiredDigitalAttribution { get; set; } =
        string.Empty;

    public string SourceSelectionReason { get; set; } =
        string.Empty;

    public string SourceSelectionEvidence { get; set; } =
        string.Empty;

    public List<string> SourceEvidenceUrls { get; set; } =
        [];

    public string SourceRightsNote { get; set; } =
        string.Empty;

    public string SemanticGraphSha256 { get; set; } =
        string.Empty;

    public PedagogicalLessonBlueprintDiagnostics
        AcquisitionDiagnostics
    { get; set; } =
        new();

    public List<PedagogicalLessonBlueprintUnit>
        Units
    { get; set; } =
        [];

    public List<PedagogicalLessonBlueprintLesson>
        Lessons
    { get; set; } =
        [];
}

public sealed class PedagogicalLessonBlueprintDiagnostics
{
    public int UnitCount { get; set; }

    public int LessonCount { get; set; }

    public int OfficialStandardCount { get; set; }

    public int AddressingCoverageCount { get; set; }

    public int FormalMappingCount { get; set; }

    public int
        LessonsWithoutNumberedGradeReferenceAnyRole
    {
        get;
        set;
    }

    public int
        LessonsWithoutNumberedAddressingStandard
    {
        get;
        set;
    }

    public int
        LessonsWithoutNumberedAddressingOrBuildingTowardsStandard
    {
        get;
        set;
    }

    public int MultiStandardLessons { get; set; }

    // Backward-compatible aliases for the already-merged Grade 6
    // pilot blueprint. Grade 6 evidence remains byte-for-byte
    // unchanged by the Grade 7/8 batch.
    [JsonPropertyName(
        "OfficialGrade6StandardCount")]
    public int LegacyOfficialGrade6StandardCount
    {
        get;
        set;
    }

    [JsonPropertyName(
        "LessonsWithoutNumberedGrade6ReferenceAnyRole")]
    public int
        LegacyLessonsWithoutNumberedGrade6ReferenceAnyRole
    {
        get;
        set;
    }

    [JsonIgnore]
    public int EffectiveOfficialStandardCount =>
        OfficialStandardCount > 0
            ? OfficialStandardCount
            : LegacyOfficialGrade6StandardCount;

    [JsonIgnore]
    public int
        EffectiveLessonsWithoutNumberedGradeReferenceAnyRole =>
        LessonsWithoutNumberedGradeReferenceAnyRole > 0
            ? LessonsWithoutNumberedGradeReferenceAnyRole
            : LegacyLessonsWithoutNumberedGrade6ReferenceAnyRole;
}

public sealed class PedagogicalLessonBlueprintUnit
{
    public int Number { get; set; }

    public string UnitCode { get; set; } =
        string.Empty;

    public int SortOrder { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public int LessonCount { get; set; }

    public string SourceUrl { get; set; } =
        string.Empty;

    public string SemanticSha256 { get; set; } =
        string.Empty;
}

public sealed class PedagogicalLessonBlueprintLesson
{
    public string SourceLessonCode { get; set; } =
        string.Empty;

    public string LessonCode { get; set; } =
        string.Empty;

    public int UnitNumber { get; set; }

    public string UnitTitle { get; set; } =
        string.Empty;

    public int LessonNumber { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public int SortOrder { get; set; }

    public string SourceUrl { get; set; } =
        string.Empty;

    public string SemanticSha256 { get; set; } =
        string.Empty;

    public List<PedagogicalLessonBlueprintAlignment>
        Alignments
    { get; set; } =
        [];

    /// <summary>
    /// Formal Edulytics mastery targets. The complete source alignment graph
    /// is preserved separately in Alignments. A source-driven lesson may have
    /// zero, one, or multiple formal mappings.
    /// </summary>
    public List<string> OutcomeCodes { get; set; } =
        [];

    public List<string> ApplicableCourses { get; set; } =
        [];

    public List<PedagogicalLessonBlueprintFormalTarget>
        FormalTargets
    { get; set; } =
        [];
}

public sealed class PedagogicalLessonBlueprintAlignment
{
    public string Role { get; set; } =
        string.Empty;

    public string ReferenceCode { get; set; } =
        string.Empty;

    public string ReferenceKind { get; set; } =
        string.Empty;

    public string ResolutionKind { get; set; } =
        string.Empty;

    public string? OutcomeCode { get; set; }

    public int SortOrder { get; set; }
}

public static class PedagogicalSourceLicensePolicy
{
    private static readonly string[] ApprovedLicenses =
    [
        "Public Domain",
        "CC0 1.0",
        "CC BY 4.0",
        "Open Government Licence v3.0"
    ];

    /// <summary>
    /// Fail-closed allowlist for pedagogical source material.
    ///
    /// A license appears here only after Edulytics has accepted it
    /// for royalty-free commercial reuse and adaptation.
    /// Unknown licenses are deliberately blocked until reviewed.
    /// </summary>
    public static IReadOnlyList<string>
        ApprovedCommercialReuseAndAdaptationLicenses =>
        ApprovedLicenses;

    public static bool IsApproved(
        string? sourceLicense)
    {
        return
            !string.IsNullOrWhiteSpace(
                sourceLicense) &&
            ApprovedLicenses.Contains(
                sourceLicense,
                StringComparer.Ordinal);
    }

    public static void Validate(
        string? sourceLicense)
    {
        if (IsApproved(
                sourceLicense))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Pedagogical source license is not approved " +
            $"for royalty-free commercial reuse and " +
            $"adaptation: " +
            $"{sourceLicense ?? "<null>"}.");
    }
}

public static class PedagogicalLessonBlueprintContract
{
    private static readonly HashSet<string> Roles =
        new(StringComparer.Ordinal)
        {
            "BuildingOn",
            "Addressing",
            "BuildingTowards"
        };

    private static readonly HashSet<string> ReferenceKinds =
        new(StringComparer.Ordinal)
        {
            "Domain",
            "Cluster",
            "NumberedStandard",
            "StandardSubpart"
        };

    private static readonly HashSet<string> ResolutionKinds =
        new(StringComparer.Ordinal)
        {
            "None",
            "ExactAcceptedStandard",
            "SubpartToAcceptedParent"
        };

    private static readonly Regex Sha256Pattern =
        new(
            "^[0-9a-f]{64}$",
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    public static void Validate(
        PedagogicalLessonBlueprintDocument document)
    {
        MathematicsCurriculumPackRegistry.Validate();

        if (document.SchemaVersion == 2)
        {
            PedagogicalLessonBlueprintV2Contract.Validate(
                document);

            return;
        }

        if (document.SchemaVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported lesson blueprint schema: " +
                $"{document.SchemaVersion}.");
        }

        Require(document.BlueprintCode, "BlueprintCode");
        Require(document.PackCode, "PackCode");
        Require(document.VersionCode, "VersionCode");
        Require(document.NativeLevel, "NativeLevel");

        Require(
            document.OfficialAuthority,
            "OfficialAuthority");

        RequireHttp(
            document.OfficialSourceUrl,
            "OfficialSourceUrl");

        Require(
            document.PedagogicalSourceType,
            "PedagogicalSourceType");

        Require(document.SourceTitle, "SourceTitle");
        Require(document.SourcePublisher, "SourcePublisher");
        Require(document.SourceEdition, "SourceEdition");

        RequireHttp(
            document.SourceRootUrl,
            "SourceRootUrl");

        Require(document.SourceLicense, "SourceLicense");

        PedagogicalSourceLicensePolicy.Validate(
            document.SourceLicense);

        Require(
            document.RequiredDigitalAttribution,
            "RequiredDigitalAttribution");

        Require(
            document.SourceSelectionReason,
            "SourceSelectionReason");

        Require(
            document.SourceSelectionEvidence,
            "SourceSelectionEvidence");

        Require(
            document.SourceRightsNote,
            "SourceRightsNote");

        RequireSha(
            document.SemanticGraphSha256,
            "SemanticGraphSha256");

        if (!DateTimeOffset.TryParse(
                document.SourceCheckedAtUtc,
                out var checkedAt) ||
            checkedAt.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "SourceCheckedAtUtc must be a valid UTC timestamp.");
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

        if (document.LogicalLevel <= 0)
        {
            throw new InvalidOperationException(
                "Blueprint LogicalLevel must be positive.");
        }

        foreach (var url in document.SourceEvidenceUrls)
        {
            RequireHttp(
                url,
                "SourceEvidenceUrls");
        }

        if (document.Units.Count == 0 ||
            document.Lessons.Count == 0)
        {
            throw new InvalidOperationException(
                "Blueprint requires units and lessons.");
        }

        var unitsByNumber =
            document.Units.ToDictionary(
                x => x.Number);

        if (unitsByNumber.Count !=
            document.Units.Count)
        {
            throw new InvalidOperationException(
                "Duplicate blueprint unit number.");
        }

        foreach (var unit in document.Units)
        {
            if (unit.Number <= 0 ||
                unit.LessonCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid blueprint unit: {unit.Number}.");
            }

            Require(
                unit.Title,
                $"Unit {unit.Number}:Title");

            RequireHttp(
                unit.SourceUrl,
                $"Unit {unit.Number}:SourceUrl");

            RequireSha(
                unit.SemanticSha256,
                $"Unit {unit.Number}:SemanticSha256");
        }

        var lessonCodes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var sourceLessonCodes =
            new HashSet<string>(
                StringComparer.Ordinal);

        var sortOrders =
            new HashSet<int>();

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
                $"{lesson.SourceLessonCode}:UnitTitle");

            Require(
                lesson.Title,
                $"{lesson.SourceLessonCode}:Title");

            RequireHttp(
                lesson.SourceUrl,
                $"{lesson.SourceLessonCode}:SourceUrl");

            RequireSha(
                lesson.SemanticSha256,
                $"{lesson.SourceLessonCode}:SemanticSha256");

            if (!lesson.LessonCode.StartsWith(
                    "PED:",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Blueprint lesson must use a PED identity: " +
                    $"{lesson.LessonCode}.");
            }

            if (!lessonCodes.Add(
                    lesson.LessonCode) ||
                !sourceLessonCodes.Add(
                    lesson.SourceLessonCode))
            {
                throw new InvalidOperationException(
                    $"Duplicate blueprint lesson identity: " +
                    $"{lesson.SourceLessonCode}.");
            }

            if (lesson.SortOrder <= 0 ||
                !sortOrders.Add(
                    lesson.SortOrder))
            {
                throw new InvalidOperationException(
                    $"Invalid/duplicate lesson SortOrder: " +
                    $"{lesson.SortOrder}.");
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
                    $"Blueprint lesson/unit drift: " +
                    $"{lesson.SourceLessonCode}.");
            }

            if (lesson.LessonNumber <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid lesson number: " +
                    $"{lesson.SourceLessonCode}.");
            }

            if (lesson.OutcomeCodes.Count !=
                lesson.OutcomeCodes
                    .Distinct(
                        StringComparer.Ordinal)
                    .Count())
            {
                throw new InvalidOperationException(
                    $"Duplicate OutcomeCode: " +
                    $"{lesson.SourceLessonCode}.");
            }

            var alignmentKeys =
                new HashSet<
                    (string Role, string ReferenceCode)>();

            var alignmentSorts =
                new HashSet<int>();

            foreach (var alignment in
                     lesson.Alignments)
            {
                Require(
                    alignment.Role,
                    $"{lesson.SourceLessonCode}:Role");

                Require(
                    alignment.ReferenceCode,
                    $"{lesson.SourceLessonCode}:ReferenceCode");

                Require(
                    alignment.ReferenceKind,
                    $"{lesson.SourceLessonCode}:ReferenceKind");

                Require(
                    alignment.ResolutionKind,
                    $"{lesson.SourceLessonCode}:ResolutionKind");

                if (!Roles.Contains(
                        alignment.Role) ||
                    !ReferenceKinds.Contains(
                        alignment.ReferenceKind) ||
                    !ResolutionKinds.Contains(
                        alignment.ResolutionKind))
                {
                    throw new InvalidOperationException(
                        $"Unsupported alignment in " +
                        $"{lesson.SourceLessonCode}: " +
                        $"{alignment.Role}/" +
                        $"{alignment.ReferenceKind}/" +
                        $"{alignment.ResolutionKind}.");
                }

                if (!alignmentKeys.Add(
                        (
                            alignment.Role,
                            alignment.ReferenceCode
                        )) ||
                    alignment.SortOrder <= 0 ||
                    !alignmentSorts.Add(
                        alignment.SortOrder))
                {
                    throw new InvalidOperationException(
                        $"Duplicate alignment in " +
                        $"{lesson.SourceLessonCode}.");
                }

                var resolved =
                    alignment.ResolutionKind is
                        "ExactAcceptedStandard" or
                        "SubpartToAcceptedParent";

                if (resolved)
                {
                    if (!string.Equals(
                            alignment.Role,
                            "Addressing",
                            StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(
                            alignment.OutcomeCode) ||
                        !lesson.OutcomeCodes.Contains(
                            alignment.OutcomeCode,
                            StringComparer.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Resolved alignment is not an " +
                            $"explicit formal Addressing mapping: " +
                            $"{lesson.SourceLessonCode}:" +
                            $"{alignment.ReferenceCode}.");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(
                             alignment.OutcomeCode))
                {
                    throw new InvalidOperationException(
                        $"Non-target alignment cannot carry " +
                        $"OutcomeCode: " +
                        $"{lesson.SourceLessonCode}:" +
                        $"{alignment.ReferenceCode}.");
                }

                if ((alignment.ReferenceKind is
                        "Cluster" or "Domain") &&
                    !string.IsNullOrWhiteSpace(
                        alignment.OutcomeCode))
                {
                    throw new InvalidOperationException(
                        $"Cluster/domain expansion forbidden: " +
                        $"{lesson.SourceLessonCode}:" +
                        $"{alignment.ReferenceCode}.");
                }
            }

            foreach (var outcomeCode in
                     lesson.OutcomeCodes)
            {
                Require(
                    outcomeCode,
                    $"{lesson.SourceLessonCode}:OutcomeCode");

                if (!lesson.Alignments.Any(
                        x =>
                            string.Equals(
                                x.Role,
                                "Addressing",
                                StringComparison.Ordinal) &&
                            string.Equals(
                                x.OutcomeCode,
                                outcomeCode,
                                StringComparison.Ordinal) &&
                            (x.ResolutionKind is
                                "ExactAcceptedStandard" or
                                "SubpartToAcceptedParent")))
                {
                    throw new InvalidOperationException(
                        $"Formal OutcomeCode lacks explicit " +
                        $"source provenance: " +
                        $"{lesson.SourceLessonCode}:" +
                        $"{outcomeCode}.");
                }
            }
        }

        foreach (var unit in document.Units)
        {
            var count =
                document.Lessons.Count(
                    x =>
                        x.UnitNumber ==
                        unit.Number);

            if (count != unit.LessonCount)
            {
                throw new InvalidOperationException(
                    $"Unit lesson-count drift: " +
                    $"U{unit.Number} expected " +
                    $"{unit.LessonCount}, got {count}.");
            }
        }

        if (sortOrders.Count !=
                document.Lessons.Count ||
            sortOrders.Min() != 1 ||
            sortOrders.Max() !=
                document.Lessons.Count)
        {
            throw new InvalidOperationException(
                "Lesson SortOrder must be continuous from 1.");
        }

        var diagnostics =
            document.AcquisitionDiagnostics;

        if (diagnostics.UnitCount !=
                document.Units.Count ||
            diagnostics.LessonCount !=
                document.Lessons.Count ||
            diagnostics.EffectiveOfficialStandardCount <= 0 ||
            diagnostics.AddressingCoverageCount !=
                diagnostics.EffectiveOfficialStandardCount ||
            diagnostics.FormalMappingCount !=
                document.Lessons.Sum(
                    x => x.OutcomeCodes.Count))
        {
            throw new InvalidOperationException(
                $"Blueprint acquisition diagnostics drift: " +
                $"{document.BlueprintCode}.");
        }
    }

    private static void Require(
        string? value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Blueprint field required: {field}.");
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
                $"Blueprint {field} must be HTTP/HTTPS.");
        }
    }

    private static void RequireSha(
        string? value,
        string field)
    {
        Require(value, field);

        if (!Sha256Pattern.IsMatch(
                value!))
        {
            throw new InvalidOperationException(
                $"Blueprint {field} must be lowercase SHA-256.");
        }
    }
}

public static class PedagogicalLessonBlueprintRegistry
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public static IReadOnlyList<
        PedagogicalLessonBlueprintDocument>
        LoadEmbeddedDocuments()
    {
        var assembly =
            typeof(
                PedagogicalLessonBlueprintRegistry)
            .Assembly;

        var names =
            assembly
                .GetManifestResourceNames()
                .Where(
                    x =>
                        x.EndsWith(
                            ".lesson-blueprint.json",
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .ToArray();

        var documents =
            new List<
                PedagogicalLessonBlueprintDocument>();

        foreach (var name in names)
        {
            using var stream =
                assembly.GetManifestResourceStream(
                    name)
                ?? throw new InvalidOperationException(
                    $"Cannot open embedded lesson blueprint: " +
                    $"{name}.");

            var document =
                JsonSerializer.Deserialize<
                    PedagogicalLessonBlueprintDocument>(
                    stream,
                    JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Invalid embedded lesson blueprint: " +
                    $"{name}.");

            PedagogicalLessonBlueprintContract.Validate(
                document);

            documents.Add(
                document);
        }

        var duplicateCode =
            documents
                .GroupBy(
                    x => x.BlueprintCode,
                    StringComparer.Ordinal)
                .FirstOrDefault(
                    x => x.Count() != 1);

        if (duplicateCode is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate BlueprintCode: " +
                $"{duplicateCode.Key}.");
        }

        var duplicateV1Scope =
            documents
                .Where(x => x.SchemaVersion == 1)
                .GroupBy(
                    x => (
                        x.PackCode,
                        x.VersionCode,
                        x.LogicalLevel,
                        x.NativeLevel,
                        x.Pathway))
                .FirstOrDefault(
                    x => x.Count() != 1);

        if (duplicateV1Scope is not null)
        {
            throw new InvalidOperationException(
                "Only one Schema V1 blueprint may own " +
                "a curriculum scope.");
        }

        var duplicateV2Scope =
            documents
                .Where(x => x.SchemaVersion == 2)
                .GroupBy(
                    x => (
                        x.PackCode,
                        x.VersionCode,
                        x.LogicalLevelFrom,
                        x.LogicalLevelTo,
                        x.Pathway,
                        x.CourseCode))
                .FirstOrDefault(
                    x => x.Count() != 1);

        if (duplicateV2Scope is not null)
        {
            throw new InvalidOperationException(
                "Only one Schema V2 blueprint may own " +
                "a course/range scope.");
        }

        return documents;
    }
}
