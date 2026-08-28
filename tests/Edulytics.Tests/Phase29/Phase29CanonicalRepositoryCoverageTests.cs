using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CanonicalRepositoryCoverageTests
{
    [Fact]
    public async Task CanonicalRepositoryReadsAdoptedCurriculumBodyOutcomesAndStudentScope()
    {
        await using var db = CreateDb();
        var ids = await SeedAsync(db);
        var repository = new LessonContentRepository(db);

        var staff = await repository.ListStaffAdoptionsAsync(ids.SchoolId);
        var staffContext = Assert.Single(staff);
        Assert.Equal(ids.FrameworkVersionId, staffContext.FrameworkVersionId);
        Assert.Equal("MATH6", staffContext.SubjectCode);
        Assert.Equal("GRADE 6", staffContext.GradeName);

        var nodes = await repository.ListCurriculumNodesAsync([ids.FrameworkVersionId]);
        Assert.Equal(2, nodes.Count);
        Assert.Contains(nodes, x => x.Id == ids.UnitNodeId && x.NodeKind == "Unit");
        Assert.Contains(nodes, x => x.Id == ids.LessonNodeId && x.NodeKind == "Lesson");

        var contents = await repository.ListCanonicalContentsAsync([ids.LessonNodeId]);
        var content = Assert.Single(contents);
        Assert.Equal(CanonicalLessonContentStatus.Published, content.Status);
        Assert.Equal("1", content.ContentVersion);

        var translation = Assert.Single(content.Translations);
        Assert.Equal("en", translation.CultureCode);
        Assert.Equal("Canonical lesson title", translation.Title);
        Assert.Equal("Explanation body", translation.Explanation);

        var outcomes = await repository.ListOfficialOutcomesAsync(
            ids.FrameworkVersionId,
            ids.LessonNodeId);

        var outcome = Assert.Single(outcomes);
        Assert.Equal(ids.StandardNodeId, outcome.Id);
        Assert.Equal("UK:6:STD:001", outcome.Code);
        Assert.Equal("Official standard text", outcome.Description);

        var student = await repository.ListStudentAdoptionsAsync(
            ids.StudentUserId,
            ids.SchoolId);

        var studentContext = Assert.Single(student);
        Assert.Equal(ids.FrameworkVersionId, studentContext.FrameworkVersionId);
        Assert.Equal(ids.GradeLevelId, studentContext.GradeLevelId);
    }

    [Fact]
    public async Task CanonicalRepositoryFailsClosedForEmptyAndUnknownScopes()
    {
        await using var db = CreateDb();
        var repository = new LessonContentRepository(db);

        Assert.Empty(
            await repository.ListCurriculumNodesAsync([]));

        Assert.Empty(
            await repository.ListCanonicalContentsAsync([]));

        Assert.Empty(
            await repository.ListOfficialOutcomesAsync(
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Empty(
            await repository.ListStaffAdoptionsAsync(
                Guid.NewGuid()));

        Assert.Empty(
            await repository.ListStudentAdoptionsAsync(
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "p29-canonical-" + Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(options);
    }

    private static async Task<SeedIds> SeedAsync(
        EdulyticsDbContext db)
    {
        var now = DateTime.UtcNow;

        var schoolId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeLevelId = Guid.NewGuid();
        var frameworkId = Guid.NewGuid();
        var frameworkVersionId = Guid.NewGuid();
        var unitNodeId = Guid.NewGuid();
        var lessonNodeId = Guid.NewGuid();
        var standardNodeId = Guid.NewGuid();
        var canonicalContentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var classGroupId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        db.Schools.Add(
            new School
            {
                Id = schoolId,
                Name = "Coverage School",
                SchoolCode = "COV",
                NormalizedSchoolCode = "COV",
                Status = SchoolStatus.Active,
                CountryCode = "GB",
                City = "London",
                ContactEmail = "coverage@example.test",
                DefaultCulture = "en",
                TimeZoneId = "UTC",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.Subjects.Add(
            new Subject
            {
                Id = subjectId,
                SchoolId = schoolId,
                Name = "Mathematics",
                Code = "MATH6",
                NormalizedCode = "MATH6",
                Status = AcademicStructureStatus.Active
            });

        db.GradeLevels.Add(
            new GradeLevel
            {
                Id = gradeLevelId,
                SchoolId = schoolId,
                Name = "GRADE 6",
                Order = 6
            });

        db.CurriculumFrameworks.Add(
            new CurriculumFramework
            {
                Id = frameworkId,
                Code = "UK",
                NormalizedCode = "UK",
                Name = "British / UK Mathematics — England",
                CountryCode = "GB",
                ProviderName = "Edulytics",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.CurriculumFrameworkVersions.Add(
            new CurriculumFrameworkVersion
            {
                Id = frameworkVersionId,
                FrameworkId = frameworkId,
                VersionCode = "2026",
                NormalizedVersionCode = "2026",
                Name = "2026",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.SchoolCurriculumAdoptions.Add(
            new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = null,
                GradeLevelId = gradeLevelId,
                SubjectId = subjectId,
                FrameworkVersionId = frameworkVersionId,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.CurriculumPackContentNodes.AddRange(
            Node(
                unitNodeId,
                frameworkVersionId,
                null,
                "Unit",
                "UK:6:UNIT:1",
                "Number",
                1,
                6,
                6,
                now),
            Node(
                lessonNodeId,
                frameworkVersionId,
                unitNodeId,
                "Lesson",
                "UK:6:LESSON:1",
                "Place value",
                2,
                6,
                6,
                now),
            Node(
                standardNodeId,
                frameworkVersionId,
                null,
                "Standard",
                "UK:6:STD:001",
                "Place value standard",
                3,
                6,
                6,
                now,
                "Official standard text"));

        db.CurriculumPackNodeLinks.Add(
            new CurriculumPackNodeLink
            {
                Id = Guid.NewGuid(),
                FrameworkVersionId = frameworkVersionId,
                FromNodeId = lessonNodeId,
                ToNodeId = standardNodeId,
                LinkKind = "LessonStandardAlignment",
                AlignmentConfidence = "Exact",
                EvidenceNote = "Coverage test alignment",
                SortOrder = 1,
                ContentHash = new string('a', 64),
                CreatedAtUtc = now
            });

        db.CurriculumLessonContents.Add(
            new CurriculumLessonContent
            {
                Id = canonicalContentId,
                FrameworkVersionId = frameworkVersionId,
                LessonNodeId = lessonNodeId,
                Status = CanonicalLessonContentStatus.Published,
                ContentVersion = "1",
                VerifiedAtUtc = now,
                PublishedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.CurriculumLessonContentTranslations.Add(
            new CurriculumLessonContentTranslation
            {
                Id = Guid.NewGuid(),
                CurriculumLessonContentId = canonicalContentId,
                CultureCode = "en",
                Title = "Canonical lesson title",
                Explanation = "Explanation body",
                KeyConceptsAndRules = "Rules body",
                WorkedExamples = "Examples body",
                StepByStepSolutions = "Solutions body",
                CommonMistakes = "Mistakes body",
                QuickSummary = "Summary body",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.StudentProfiles.Add(
            new StudentProfile
            {
                Id = studentProfileId,
                SchoolId = schoolId,
                UserId = studentUserId,
                StudentNumber = "S001",
                NormalizedStudentNumber = "S001",
                FirstName = "Coverage",
                LastName = "Student",
                DisplayName = "Coverage Student",
                Status = AcademicStructureStatus.Active,
                IsArchived = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.ClassGroups.Add(
            new ClassGroup
            {
                Id = classGroupId,
                SchoolId = schoolId,
                AcademicYearId = academicYearId,
                GradeLevelId = gradeLevelId,
                Name = "A-1",
                Code = "A-1",
                NormalizedCode = "A-1",
                Status = AcademicStructureStatus.Active
            });

        db.StudentEnrollments.Add(
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                StudentProfileId = studentProfileId,
                ClassGroupId = classGroupId,
                AcademicYearId = academicYearId,
                EnrolledAtUtc = now
            });

        await db.SaveChangesAsync();

        return new SeedIds(
            schoolId,
            subjectId,
            gradeLevelId,
            frameworkVersionId,
            unitNodeId,
            lessonNodeId,
            standardNodeId,
            studentUserId);
    }

    private static CurriculumPackContentNode Node(
        Guid id,
        Guid frameworkVersionId,
        Guid? parentId,
        string kind,
        string code,
        string title,
        int sortOrder,
        int levelFrom,
        int levelTo,
        DateTime now,
        string? officialText = null) =>
        new()
        {
            Id = id,
            FrameworkVersionId = frameworkVersionId,
            FrameworkCode = "UK",
            VersionCode = "2026",
            NodeKind = kind,
            Code = code,
            ParentId = parentId,
            LogicalLevelFrom = levelFrom,
            LogicalLevelTo = levelTo,
            NativeLevel = "Year 6",
            Title = title,
            OfficialText = officialText,
            SourceAuthority = "Official authority",
            SourceUrl = "https://example.test/source",
            SourceLocator = "coverage",
            Attribution = "coverage",
            IsOfficial = true,
            IsActive = true,
            SortOrder = sortOrder,
            ContentHash = new string('b', 64),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private sealed record SeedIds(
        Guid SchoolId,
        Guid SubjectId,
        Guid GradeLevelId,
        Guid FrameworkVersionId,
        Guid UnitNodeId,
        Guid LessonNodeId,
        Guid StandardNodeId,
        Guid StudentUserId);
}
