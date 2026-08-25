namespace Edulytics.Core.Curriculum;

public enum CurriculumReuseBasis
{
    OpenGovernmentLicenceV3 = 1,
    ProductOwnerConfirmedCommercialUseEvidence = 2,
    OfficialGovernmentSourceReference = 3,
    OfficialLegalOrGovernmentTextReference = 4
}

public enum CurriculumTextMode
{
    FullOfficialTextPermitted = 1,
    OfficialSourceLinked = 2
}

public sealed record CurriculumSourceReference(
    string Purpose,
    string Authority,
    string Url,
    string VersionLabel,
    bool OfficialAuthority);

public sealed record AcademicLevelMapping(
    int LogicalLevel,
    string NativeLabel,
    string Stage,
    string? Pathway,
    bool CoveredByRegisteredSource);

public sealed record MathematicsCurriculumPackDefinition(
    string Code,
    string DisplayName,
    string CountryCode,
    string SubjectCode,
    CurriculumReuseBasis ReuseBasis,
    CurriculumTextMode TextMode,
    string EvidenceNote,
    string RequiredAttribution,
    IReadOnlyList<CurriculumSourceReference> Sources,
    IReadOnlyList<AcademicLevelMapping> Levels);

public sealed record MathematicsLessonBlueprint(
    string PackCode,
    int LogicalLevel,
    string NativeLevel,
    string UnitKey,
    string LessonKey,
    string LessonTitle,
    string StandardsLinkRule);

public static class MathematicsCurriculumPackHierarchy
{
    public static IReadOnlyList<string> OrderedLevels { get; } =
    [
        "CurriculumFramework",
        "CurriculumFrameworkVersion",
        "AcademicLevel",
        "Mathematics",
        "DomainOrStrand",
        "StandardOrLearningOutcome",
        "Unit",
        "Lesson",
        "ActivityOrAssessmentQuestion"
    ];
}

public static class MathematicsCurriculumPackRegistry
{
    public const string MathematicsSubjectCode = "MATH";
    public const string EnglandCode = "UK-NC-ENG-MATH";
    public const string CommonCoreCode = "US-CCSS-MATH";
    public const string UaeCode = "UAE-MOE-MATH";
    public const string PolandCode = "PL-NATIONAL-MATH";

    public const string CommonCoreAttribution =
        "© Copyright 2010. National Governors Association Center for Best Practices and Council of Chief State School Officers. All rights reserved.";

    public static IReadOnlyList<MathematicsCurriculumPackDefinition> All { get; } =
    [
        new(
            EnglandCode,
            "British / UK Mathematics — England",
            "GB-ENG",
            MathematicsSubjectCode,
            CurriculumReuseBasis.OpenGovernmentLicenceV3,
            CurriculumTextMode.FullOfficialTextPermitted,
            "DfE Mathematics programmes of study are published under OGL v3.0 except third-party material. Years 12-13 use the separate DfE AS/A-level Mathematics subject-content source.",
            "Contains public sector information licensed under the Open Government Licence v3.0.",
            [
                new(
                    "Years 1-11 statutory Mathematics programmes of study",
                    "UK Department for Education",
                    "https://www.gov.uk/government/publications/national-curriculum-in-england-mathematics-programmes-of-study/national-curriculum-in-england-mathematics-programmes-of-study",
                    "National Curriculum KS1-KS4; updated 28 September 2021",
                    true),
                new(
                    "Years 12-13 post-16 Mathematics subject content",
                    "UK Department for Education",
                    "https://www.gov.uk/government/publications/gce-as-and-a-level-mathematics",
                    "AS and A level Mathematics subject content",
                    true),
                new(
                    "Reuse licence",
                    "The National Archives",
                    "https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/",
                    "Open Government Licence v3.0",
                    true)
            ],
            [
                new(1,"Year 1","Key Stage 1",null,true),
                new(2,"Year 2","Key Stage 1",null,true),
                new(3,"Year 3","Key Stage 2",null,true),
                new(4,"Year 4","Key Stage 2",null,true),
                new(5,"Year 5","Key Stage 2",null,true),
                new(6,"Year 6","Key Stage 2",null,true),
                new(7,"Year 7","Key Stage 3",null,true),
                new(8,"Year 8","Key Stage 3",null,true),
                new(9,"Year 9","Key Stage 3",null,true),
                new(10,"Year 10","Key Stage 4",null,true),
                new(11,"Year 11","Key Stage 4",null,true),
                new(12,"Year 12","Post-16","AS/A-level Mathematics",true),
                new(13,"Year 13","Post-16","A-level Mathematics",true)
            ]),

        new(
            CommonCoreCode,
            "American Mathematics — Common Core State Standards",
            "US",
            MathematicsSubjectCode,
            CurriculumReuseBasis.ProductOwnerConfirmedCommercialUseEvidence,
            CurriculumTextMode.FullOfficialTextPermitted,
            "Commercial-use evidence is explicitly confirmed by the Edulytics product owner. Edulytics records that confirmation without inventing a licence identifier or effective date.",
            CommonCoreAttribution,
            [
                new(
                    "Official Mathematics standards",
                    "Common Core State Standards Initiative / NGA Center / CCSSO",
                    "https://corestandards.org/mathematics-standards/",
                    "Common Core State Standards for Mathematics",
                    true),
                new(
                    "Official Mathematics standards PDF",
                    "Common Core State Standards Initiative / NGA Center / CCSSO",
                    "https://corestandards.org/wp-content/uploads/2023/09/Math_Standards1.pdf",
                    "Common Core State Standards for Mathematics PDF",
                    true),
                new(
                    "Published commercial-use terms reference",
                    "NGA Center / CCSSO",
                    "https://www.thecorestandards.org/commercial-license/",
                    "Commercial License terms reference",
                    true)
            ],
            [
                new(1,"Kindergarten","K-8",null,true),
                new(2,"Grade 1","K-8",null,true),
                new(3,"Grade 2","K-8",null,true),
                new(4,"Grade 3","K-8",null,true),
                new(5,"Grade 4","K-8",null,true),
                new(6,"Grade 5","K-8",null,true),
                new(7,"Grade 6","K-8",null,true),
                new(8,"Grade 7","K-8",null,true),
                new(9,"Grade 8","K-8",null,true),
                new(10,"Grade 9","High School","Course/pathway mapping",true),
                new(11,"Grade 10","High School","Course/pathway mapping",true),
                new(12,"Grade 11","High School","Course/pathway mapping",true),
                new(13,"Grade 12","High School","Course/pathway mapping",true)
            ]),

        new(
            UaeCode,
            "UAE Ministry of Education Mathematics",
            "AE",
            MathematicsSubjectCode,
            CurriculumReuseBasis.OfficialGovernmentSourceReference,
            CurriculumTextMode.OfficialSourceLinked,
            "Edulytics uses UAE MoE Mathematics 2026-2027 as the user-facing curriculum version. Current Term 1 source-catalog evidence establishes Grade/track scope. Historical UAE MoE standard codes are internal provenance and may align to current lessons only under the Product Owner EXACT historical-outcome rule; codes are never invented.",
            "Source: UAE Ministry of Education. Preserve source/version metadata on every imported official item.",
            [
                new(
                    "Official Ministry authority",
                    "UAE Ministry of Education",
                    "https://www.moe.gov.ae/",
                    "Current official Ministry portal",
                    true),
                new(
                    "Current Mathematics ebook/source platform",
                    "UAE Ministry of Education",
                    "https://minhaji.moe.gov.ae/",
                    "Academic year 2026-2027 / Term 1 current source catalog",
                    true),
                new(
                    "Current assessment/scope metadata only",
                    "UAE Ministry of Education",
                    "https://www.moe.gov.ae/en/guides/Pages/Student-Assessment-Policy-Guide-2025-2026.aspx",
                    "Student Assessment Policy Guide 2025-2026",
                    true),
                new(
                    "Historical official Mathematics curriculum standards framework reference",
                    "UAE Ministry of Education",
                    "https://www.moe.gov.ae/Ar/ImportantLinks/Assessment/Pages/Curriculum-Docs.aspx",
                    "Mathematics Curriculum Standards Framework 2017 reference",
                    true)
            ],
            [
                new(1,"Grade 1","Cycle 1",null,true),
                new(2,"Grade 2","Cycle 1",null,true),
                new(3,"Grade 3","Cycle 1",null,true),
                new(4,"Grade 4","Cycle 1",null,true),
                new(5,"Grade 5","Cycle 2",null,true),
                new(6,"Grade 6","Cycle 2",null,true),
                new(7,"Grade 7","Cycle 2",null,true),
                new(8,"Grade 8","Cycle 2",null,true),
                new(9,"Grade 9","Secondary","Preserve current pathway metadata",true),
                new(10,"Grade 10","Secondary","Preserve current pathway metadata",true),
                new(11,"Grade 11","Secondary","Preserve current pathway metadata",true),
                new(12,"Grade 12","Secondary","Preserve current pathway metadata",true)
            ]),

        new(
            PolandCode,
            "Polish National Curriculum Mathematics",
            "PL",
            MathematicsSubjectCode,
            CurriculumReuseBasis.OfficialLegalOrGovernmentTextReference,
            CurriculumTextMode.OfficialSourceLinked,
            "Official ZPE curriculum pages are registered for the 2025/2026 school year. Native upper-secondary pathways are preserved rather than flattened.",
            "Source: Zintegrowana Platforma Edukacyjna / Polish education authorities.",
            [
                new(
                    "Early education curriculum",
                    "Zintegrowana Platforma Edukacyjna",
                    "https://zpe.gov.pl/podstawa-programowa/edukacja-wczesnoszkolna",
                    "School year 2025/2026",
                    true),
                new(
                    "Primary Mathematics IV-VIII",
                    "Zintegrowana Platforma Edukacyjna",
                    "https://zpe.gov.pl/podstawa-programowa/szkola-podstawowa/matematyka",
                    "School year 2025/2026",
                    true),
                new(
                    "Upper-secondary Mathematics",
                    "Zintegrowana Platforma Edukacyjna",
                    "https://zpe.gov.pl/podstawa-programowa/szkola-ponadpodstawowa/matematyka",
                    "School year 2025/2026",
                    true)
            ],
            [
                new(1,"Klasa I","Szkoła podstawowa","Edukacja wczesnoszkolna",true),
                new(2,"Klasa II","Szkoła podstawowa","Edukacja wczesnoszkolna",true),
                new(3,"Klasa III","Szkoła podstawowa","Edukacja wczesnoszkolna",true),
                new(4,"Klasa IV","Szkoła podstawowa",null,true),
                new(5,"Klasa V","Szkoła podstawowa",null,true),
                new(6,"Klasa VI","Szkoła podstawowa",null,true),
                new(7,"Klasa VII","Szkoła podstawowa",null,true),
                new(8,"Klasa VIII","Szkoła podstawowa",null,true),
                new(9,"Klasa I","Szkoła ponadpodstawowa","Liceum ogólnokształcące",true),
                new(10,"Klasa II","Szkoła ponadpodstawowa","Liceum ogólnokształcące",true),
                new(11,"Klasa III","Szkoła ponadpodstawowa","Liceum ogólnokształcące",true),
                new(12,"Klasa IV","Szkoła ponadpodstawowa","Liceum ogólnokształcące",true),
                new(9,"Klasa I","Szkoła ponadpodstawowa","Technikum",true),
                new(10,"Klasa II","Szkoła ponadpodstawowa","Technikum",true),
                new(11,"Klasa III","Szkoła ponadpodstawowa","Technikum",true),
                new(12,"Klasa IV","Szkoła ponadpodstawowa","Technikum",true),
                new(13,"Klasa V","Szkoła ponadpodstawowa","Technikum",true)
            ])
    ];

    public static void Validate()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            EnglandCode,
            CommonCoreCode,
            UaeCode,
            PolandCode
        };

        if (All.Count != 4 || !expected.SetEquals(All.Select(x => x.Code)))
            throw new InvalidOperationException("Exactly four approved Mathematics packs are required.");

        foreach (var pack in All)
        {
            if (!string.Equals(pack.SubjectCode, MathematicsSubjectCode, StringComparison.Ordinal))
                throw new InvalidOperationException($"Only Mathematics is allowed: {pack.Code}");

            if (pack.Levels.Count == 0 || pack.Levels.Any(x => x.LogicalLevel is < 1 or > 13))
                throw new InvalidOperationException($"Logical levels must stay inside 1..13: {pack.Code}");

            if (pack.Sources.Count == 0)
                throw new InvalidOperationException($"At least one source is required: {pack.Code}");

            foreach (var source in pack.Sources)
            {
                if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException($"HTTPS source required: {source.Url}");
                }

                if (string.IsNullOrWhiteSpace(source.Authority) ||
                    string.IsNullOrWhiteSpace(source.VersionLabel))
                {
                    throw new InvalidOperationException($"Source provenance incomplete: {pack.Code}");
                }
            }
        }

        var us = All.Single(x => x.Code == CommonCoreCode);
        if (us.ReuseBasis != CurriculumReuseBasis.ProductOwnerConfirmedCommercialUseEvidence ||
            us.TextMode != CurriculumTextMode.FullOfficialTextPermitted ||
            !us.RequiredAttribution.Contains("Copyright 2010", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Common Core product-owner evidence/attribution contract is incomplete.");
        }

        if (!Enumerable.Range(1, 13).SequenceEqual(
                us.Levels.Select(x => x.LogicalLevel).Distinct().OrderBy(x => x)))
        {
            throw new InvalidOperationException("Common Core K-12 must map to logical levels 1..13.");
        }

        var uae = All.Single(x => x.Code == UaeCode);
        if (uae.Levels.Max(x => x.LogicalLevel) != 12 ||
            uae.Levels.Any(x => x.LogicalLevel == 13))
        {
            throw new InvalidOperationException("UAE must stop at Grade 12.");
        }

        var uk = All.Single(x => x.Code == EnglandCode);
        if (!uk.Levels.Any(x => x.LogicalLevel == 13 && x.NativeLabel == "Year 13"))
            throw new InvalidOperationException("England pack must include post-16 Year 13 mapping.");

        var pl = All.Single(x => x.Code == PolandCode);
        if (!pl.Levels.Any(x =>
                x.LogicalLevel == 13 &&
                x.Pathway == "Technikum" &&
                x.NativeLabel == "Klasa V"))
        {
            throw new InvalidOperationException("Poland logical level 13 is Technikum Klasa V.");
        }
    }
}

public static class MathematicsLessonBlueprintRegistry
{
    private static readonly IReadOnlyDictionary<string, string[]> UnitFamilies =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [MathematicsCurriculumPackRegistry.EnglandCode] =
                ["Number", "Algebra", "RatioAndProportion", "Geometry", "Measurement", "Statistics"],
            [MathematicsCurriculumPackRegistry.CommonCoreCode] =
                ["CountingAndCardinality", "OperationsAndAlgebraicThinking", "Number", "Geometry", "MeasurementAndData", "Functions", "StatisticsAndProbability"],
            [MathematicsCurriculumPackRegistry.UaeCode] =
                ["NumbersAndOperations", "Algebra", "GeometryAndMeasurement", "DataAndProbability", "MathematicalReasoning"],
            [MathematicsCurriculumPackRegistry.PolandCode] =
                ["Liczby", "Algebra", "Geometria", "ObliczeniaPraktyczne", "Statystyka", "Rozumowanie"]
        };

    public static IReadOnlyList<MathematicsLessonBlueprint> CreateBlueprints()
    {
        var result = new List<MathematicsLessonBlueprint>();

        foreach (var pack in MathematicsCurriculumPackRegistry.All)
        {
            var units = UnitFamilies[pack.Code];

            foreach (var level in pack.Levels
                         .GroupBy(x => new { x.LogicalLevel, x.NativeLabel, x.Pathway })
                         .Select(x => x.First()))
            {
                var order = 0;
                foreach (var unit in units)
                {
                    order++;
                    result.Add(new MathematicsLessonBlueprint(
                        pack.Code,
                        level.LogicalLevel,
                        level.NativeLabel,
                        $"{pack.Code}:L{level.LogicalLevel}:{unit}",
                        $"{pack.Code}:L{level.LogicalLevel}:{unit}:LESSON-01",
                        $"{level.NativeLabel} — {unit}",
                        "Every production lesson must reference one or more standards/learning outcomes from the same framework version, subject and academic-level scope."));
                }
            }
        }

        return result;
    }

    public static void Validate()
    {
        var blueprints = CreateBlueprints();

        if (blueprints.Count == 0)
            throw new InvalidOperationException("Lesson blueprint registry cannot be empty.");

        foreach (var pack in MathematicsCurriculumPackRegistry.All)
        {
            if (!blueprints.Any(x => x.PackCode == pack.Code))
                throw new InvalidOperationException($"Lesson blueprint missing for {pack.Code}");
        }

        if (blueprints.Any(x => string.IsNullOrWhiteSpace(x.StandardsLinkRule)))
            throw new InvalidOperationException("Every lesson blueprint needs a standards-link rule.");
    }
}
