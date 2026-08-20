using System.Globalization;
using System.IO.Compression;
using System.Text;
using Edulytics.Core.Analytics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Reports;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Services.Analytics;
using Edulytics.Services.Auditing;
using Edulytics.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase20;

public sealed class Phase20ReportTests
{
    [Fact]
    public async Task SchoolAdmin_CanBuildSchoolReport()
    {
        var f = Fixture.Create();

        var result =
            await f.Reports.BuildAsync(
                f.Admin.Id,
                new ReportRequest(
                    ReportKind.School),
                500);

        var document =
            Assert.IsType<ReportDocument>(
                result.Value);

        Assert.Equal(
            ReportKind.School,
            document.Kind);

        Assert.Single(document.Rows);
    }

    [Fact]
    public async Task Teacher_CannotExportUnassignedSubject()
    {
        var f = Fixture.Create();

        var result =
            await f.Reports.ValidateAsync(
                f.Teacher.Id,
                new ReportRequest(
                    ReportKind.Subject,
                    SubjectId:
                        f.SubjectB.Id));

        Assert.Null(result.Value);

        Assert.Equal(
            ReportErrorCode.AccessDenied,
            result.Error);
    }

    [Fact]
    public async Task Supervisor_IsRestrictedToAssignedSubject()
    {
        var f = Fixture.Create();

        var allowed =
            await f.Reports.BuildAsync(
                f.Supervisor.Id,
                new ReportRequest(
                    ReportKind.Subject,
                    SubjectId:
                        f.SubjectA.Id),
                500);

        Assert.NotNull(
            allowed.Value);

        var denied =
            await f.Reports.BuildAsync(
                f.Supervisor.Id,
                new ReportRequest(
                    ReportKind.Subject,
                    SubjectId:
                        f.SubjectB.Id),
                500);

        Assert.Null(
            denied.Value);

        Assert.Equal(
            ReportErrorCode.AccessDenied,
            denied.Error);
    }

    [Fact]
    public async Task Teacher_CannotSelectStudentOutsideAssignment()
    {
        var f = Fixture.Create();

        var allowed =
            await f.Reports.BuildAsync(
                f.Teacher.Id,
                new ReportRequest(
                    ReportKind.Student,
                    StudentProfileId:
                        f.StudentA.Id),
                500);

        Assert.NotNull(
            allowed.Value);

        var denied =
            await f.Reports.BuildAsync(
                f.Teacher.Id,
                new ReportRequest(
                    ReportKind.Student,
                    StudentProfileId:
                        f.StudentB.Id),
                500);

        Assert.Null(
            denied.Value);

        Assert.Equal(
            ReportErrorCode.AccessDenied,
            denied.Error);
    }

    [Fact]
    public async Task NonAdmin_CannotBuildSchoolWideReport()
    {
        var f = Fixture.Create();

        var teacher =
            await f.Reports.BuildAsync(
                f.Teacher.Id,
                new ReportRequest(
                    ReportKind.School),
                500);

        Assert.Equal(
            ReportErrorCode.AccessDenied,
            teacher.Error);

        var supervisor =
            await f.Reports.BuildAsync(
                f.Supervisor.Id,
                new ReportRequest(
                    ReportKind.School),
                500);

        Assert.Equal(
            ReportErrorCode.AccessDenied,
            supervisor.Error);
    }

    [Fact]
    public void FormulaInjectionGuard_ProtectsDangerousText()
    {
        Assert.Equal(
            "'=2+2",
            SpreadsheetTextGuard
                .Sanitize("=2+2"));

        Assert.Equal(
            "' +SUM(A1:A2)",
            SpreadsheetTextGuard
                .Sanitize(
                    " +SUM(A1:A2)"));

        Assert.Equal(
            "Safe text",
            SpreadsheetTextGuard
                .Sanitize(
                    "Safe text"));
    }

    [Fact]
    public async Task CsvAndXlsx_AreSafeAndValid()
    {
        var document =
            new ReportDocument(
                ReportKind.Subject,
                "Title",
                DateTime.UtcNow,
                [
                    new(
                        "ColumnName",
                        ReportCellKind.Text)
                ],
                [
                    new(
                        [
                            ReportCell.Text(
                                "=2+2")
                        ])
                ],
                1,
                false);

        var csv =
            await ReportExportRenderer
                .RenderAsync(
                    document,
                    ReportExportFormat.Csv,
                    key => key,
                    CultureInfo.InvariantCulture);

        var csvText =
            Encoding.UTF8.GetString(
                csv.Content);

        Assert.Contains(
            "'=2+2",
            csvText);

        var xlsx =
            await ReportExportRenderer
                .RenderAsync(
                    document,
                    ReportExportFormat.Xlsx,
                    key => key,
                    CultureInfo.InvariantCulture);

        using var zip =
            new ZipArchive(
                new MemoryStream(
                    xlsx.Content),
                ZipArchiveMode.Read);

        Assert.NotNull(
            zip.GetEntry(
                "xl/workbook.xml"));

        var sheet =
                zip.GetEntry(
                    "xl/worksheets/sheet1.xml");

            Assert.NotNull(sheet);

            using var reader =
                new StreamReader(
                    sheet!.Open());

        var xml =
            reader.ReadToEnd();

        Assert.DoesNotContain(
            "<f",
            xml,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "'=2+2",
            xml);
    }

    [Fact]
    public async Task ExportRequest_IsDurableOutboxAndAudited()
    {
        var f = Fixture.Create();

        var result =
            await f.Exports.RequestAsync(
                f.Admin.Id,
                new ReportRequest(
                    ReportKind.School),
                ReportExportFormat.Csv,
                "en");

        Assert.True(
            result.Succeeded);

        var job =
            Assert.Single(
                f.ExportRepository.Jobs);

        var outbox =
            Assert.Single(
                f.ExportRepository.Outbox);

        Assert.Equal(
            ReportEventTypes.ExportRequested,
            outbox.EventType);

        Assert.Contains(
            job.Id.ToString(),
            outbox.PayloadJson);

        Assert.DoesNotContain(
            f.StudentA.DisplayName,
            outbox.PayloadJson);

        var audit =
            Assert.Single(
                f.Audit.Events);

        Assert.Equal(
            "Report.ExportRequested",
            audit.Action);
    }

    [Fact]
    public async Task Download_IsOwnerScoped_AndAudited()
    {
        var f = Fixture.Create();

        var request =
            await f.Exports.RequestAsync(
                f.Admin.Id,
                new ReportRequest(
                    ReportKind.School),
                ReportExportFormat.Csv,
                "en");

        var id =
            Assert.IsType<Guid>(
                request.Id);

        var job =
            Assert.Single(
                f.ExportRepository.Jobs);

        job.Status =
            ReportExportJobStatus.Completed;

        job.FileName =
            "report.csv";

        job.ContentType =
            "text/csv";

        job.FileContent =
            [1, 2, 3];

        job.RowCount = 1;

        var owner =
            await f.Exports.DownloadAsync(
                f.Admin.Id,
                id);

        Assert.NotNull(owner.Value);

        Assert.Contains(
            f.Audit.Events,
            x =>
                x.Action ==
                "Report.Downloaded");

        var otherAdmin =
            NewUser(
                f.School.Id,
                RoleNames.SchoolAdmin);

        f.Users.Seed(otherAdmin);

        var denied =
            await f.Exports.DownloadAsync(
                otherAdmin.Id,
                id);

        Assert.Null(
            denied.Value);

        Assert.Equal(
            ReportErrorCode.NotFound,
            denied.Error);
    }

    [Fact]
    public async Task HtmlPreview_IsBounded()
    {
        var f = Fixture.Create();

        for (var i = 0; i < 20; i++)
        {
            f.Analytics.Projection
                .ClassOutcomeSummaries
                .Add(
                    NewSummary(
                        f.School.Id,
                        f.Year.Id,
                        f.ClassA.Id,
                        f.SubjectA.Id,
                        f.OutcomeA.Id,
                        DateTime.UtcNow));
        }

        var result =
            await f.Reports.BuildAsync(
                f.Admin.Id,
                new ReportRequest(
                    ReportKind.Class,
                    ClassGroupId:
                        f.ClassA.Id),
                5);

        var document =
            Assert.IsType<ReportDocument>(
                result.Value);

        Assert.True(
            document.Truncated);

        Assert.Equal(
            5,
            document.Rows.Count);

        Assert.True(
            document.TotalRowCount > 5);
    }

    [Fact]
    public void Model_HasTenantScopedExportJob()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid()
                        .ToString())
                .Options;

        using var db =
            new EdulyticsDbContext(
                options);

        var entity =
            db.Model.FindEntityType(
                typeof(
                    ReportExportJob));

        Assert.NotNull(entity);

        Assert.Contains(
                entity!
                    .GetIndexes(),
                index =>
                    index.Properties
                        .Select(x => x.Name)
                        .SequenceEqual(
                            new[]
                            {
                                "SchoolId",
                                "RequestedByUserId",
                                "CreatedAtUtc"
                            }));

        Assert.NotNull(
            entity.FindProperty(
                nameof(
                    ReportExportJob
                        .RowVersion)));
    }

    [Fact]
    public void WebContract_HasSecurityAndLocalization()
    {
        var root =
            FindRoot();

        var controller =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "ReportsController.cs"));

        Assert.Contains(
            "[Authorize(Policy = \"ReportRead\")]",
            controller);

        Assert.Contains(
            "[ValidateAntiForgeryToken]",
            controller);

        Assert.Contains(
            "ReportExportRate",
            controller);

        Assert.Contains(
            "ReportConcurrency",
            controller);

        var view =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Reports",
                    "Index.cshtml"));

        Assert.Contains(
            "_idempotencyKey",
            view);

        Assert.Contains(
            "data-print-report",
            view);

        Assert.DoesNotContain(
            "onclick=",
            view,
            StringComparison.OrdinalIgnoreCase);

        var siteJs =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "wwwroot",
                    "js",
                    "site.js"));

        Assert.Contains(
            "wirePrintButtons",
            siteJs);

        Assert.Contains(
            "globalThis.print()",
            siteJs);

        Assert.Contains(
            "data-label",
            view);
    }

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (
            directory is not null &&
            !File.Exists(
                Path.Combine(
                    directory.FullName,
                    "Edulytics.sln")))
        {
            directory =
                directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Repository root not found.");
    }

    private static SchoolUserRecord NewUser(
        Guid schoolId,
        string role) =>
        new(
            Guid.NewGuid(),
            schoolId,
            $"{Guid.NewGuid():N}@example.com",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private static ClassOutcomeSummary
        NewSummary(
            Guid schoolId,
            Guid yearId,
            Guid classId,
            Guid subjectId,
            Guid outcomeId,
            DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = yearId,
            ClassGroupId = classId,
            SubjectId = subjectId,
            LearningOutcomeId = outcomeId,
            EarnedScore = 8m,
            PossibleScore = 10m,
            AverageMasteryPercentage = 80m,
            StudentCount = 1,
            AtRiskStudentCount = 0,
            EvidenceCount = 1,
            CalculatedAtUtc = now
        };

    private sealed class Fixture
    {
        public required School School { get; init; }
        public required SchoolUserRecord Admin { get; init; }
        public required SchoolUserRecord Teacher { get; init; }
        public required SchoolUserRecord Supervisor { get; init; }
        public required AcademicYear Year { get; init; }
        public required ClassGroup ClassA { get; init; }
        public required ClassGroup ClassB { get; init; }
        public required Subject SubjectA { get; init; }
        public required Subject SubjectB { get; init; }
        public required StudentProfile StudentA { get; init; }
        public required StudentProfile StudentB { get; init; }
        public required LearningOutcome OutcomeA { get; init; }
        public required LearningOutcome OutcomeB { get; init; }
        public required FakeUserRepository Users { get; init; }
        public required FakeAnalyticsRepository Analytics { get; init; }
        public required FakeExportRepository ExportRepository { get; init; }
        public required FakeAuditService Audit { get; init; }
        public required ReportQueryService Reports { get; init; }
        public required ReportExportService Exports { get; init; }

        public static Fixture Create()
        {
            var now =
                DateTime.UtcNow;

            var school =
                new School
                {
                    Id = Guid.NewGuid(),
                    Name = "Phase20 School",
                    SchoolCode = "P20",
                    NormalizedSchoolCode = "P20",
                    Status = SchoolStatus.Active,
                    CountryCode = "PL",
                    City = "Warsaw",
                    ContactEmail =
                        "school@example.com",
                    DefaultCulture = "en",
                    TimeZoneId =
                        "Europe/Warsaw",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            var admin =
                NewUser(
                    school.Id,
                    RoleNames.SchoolAdmin);

            var teacher =
                NewUser(
                    school.Id,
                    RoleNames.Teacher);

            var supervisor =
                NewUser(
                    school.Id,
                    RoleNames.SubjectSupervisor);

            var year =
                new AcademicYear
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    Name = "2026/27",
                    StartsOn =
                        new DateOnly(2026, 9, 1),
                    EndsOn =
                        new DateOnly(2027, 6, 30),
                    Status =
                        AcademicStructureStatus.Active
                };

            var classA =
                new ClassGroup
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    AcademicYearId = year.Id,
                    GradeLevelId = Guid.NewGuid(),
                    Name = "Class A",
                    Code = "A",
                    NormalizedCode = "A",
                    Status =
                        AcademicStructureStatus.Active
                };

            var classB =
                new ClassGroup
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    AcademicYearId = year.Id,
                    GradeLevelId = Guid.NewGuid(),
                    Name = "Class B",
                    Code = "B",
                    NormalizedCode = "B",
                    Status =
                        AcademicStructureStatus.Active
                };

            var subjectA =
                new Subject
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    Name = "Biology",
                    Code = "BIO",
                    NormalizedCode = "BIO",
                    Status =
                        AcademicStructureStatus.Active
                };

            var subjectB =
                new Subject
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    Name = "Chemistry",
                    Code = "CHE",
                    NormalizedCode = "CHE",
                    Status =
                        AcademicStructureStatus.Active
                };

            var studentA =
                new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    StudentNumber = "S-A",
                    NormalizedStudentNumber = "S-A",
                    FirstName = "Student",
                    LastName = "A",
                    DisplayName = "Student A",
                    Status =
                        AcademicStructureStatus.Active,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            var studentB =
                new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    StudentNumber = "S-B",
                    NormalizedStudentNumber = "S-B",
                    FirstName = "Student",
                    LastName = "B",
                    DisplayName = "Student B",
                    Status =
                        AcademicStructureStatus.Active,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            var outcomeA =
                NewOutcome(
                    school.Id,
                    subjectA.Id,
                    "BIO-1");

            var outcomeB =
                NewOutcome(
                    school.Id,
                    subjectB.Id,
                    "CHE-1");

            var projection =
                new AnalyticsProjectionSnapshot(
                    [year],
                    [classA, classB],
                    [subjectA, subjectB],
                    [studentA, studentB],
                    [
                        new TeacherAssignment
                        {
                            Id = Guid.NewGuid(),
                            SchoolId = school.Id,
                            TeacherUserId =
                                teacher.Id,
                            AcademicYearId =
                                year.Id,
                            ClassGroupId =
                                classA.Id,
                            SubjectId =
                                subjectA.Id,
                            CreatedAtUtc = now
                        }
                    ],
                    [],
                    [outcomeA, outcomeB],
                    [
                        NewMastery(
                            school.Id,
                            year.Id,
                            classA.Id,
                            subjectA.Id,
                            studentA.Id,
                            outcomeA.Id,
                            now),

                        NewMastery(
                            school.Id,
                            year.Id,
                            classB.Id,
                            subjectB.Id,
                            studentB.Id,
                            outcomeB.Id,
                            now)
                    ],
                    [
                        NewSummary(
                            school.Id,
                            year.Id,
                            classA.Id,
                            subjectA.Id,
                            outcomeA.Id,
                            now),

                        NewSummary(
                            school.Id,
                            year.Id,
                            classB.Id,
                            subjectB.Id,
                            outcomeB.Id,
                            now)
                    ],
                    [],
                    [],
                    [
                        new SchoolAnalyticsSnapshot
                        {
                            Id = Guid.NewGuid(),
                            SchoolId = school.Id,
                            AcademicYearId =
                                year.Id,
                            OverallMasteryPercentage =
                                80m,
                            StudentsWithEvidence = 2,
                            AtRiskStudents = 0,
                            CriticalOutcomeCount = 0,
                            WeakTopicCount = 0,
                            CalculatedAtUtc = now
                        }
                    ]);

            var users =
                new FakeUserRepository();

            users.Seed(admin);
            users.Seed(teacher);
            users.Seed(supervisor);

            var schools =
                new FakeSchoolRepository();

            schools.Seed(school);

            var analytics =
                new FakeAnalyticsRepository(
                    projection);

            var assignments =
                new FakeAssignmentRepository();

            assignments.Subjects.AddRange(
                [subjectA, subjectB]);

            assignments.Assignments.Add(
                new SubjectSupervisorAssignment
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    SupervisorUserId =
                        supervisor.Id,
                    SubjectId =
                        subjectA.Id,
                    CreatedAtUtc = now
                });

            var reports =
                new ReportQueryService(
                    analytics,
                    schools,
                    users,
                    assignments);

            var exportRepository =
                new FakeExportRepository();

            var audit =
                new FakeAuditService();

            var exports =
                new ReportExportService(
                    reports,
                    exportRepository,
                    audit,
                    new FakeMetadataProvider(),
                    new ReportOptions());

            return new Fixture
            {
                School = school,
                Admin = admin,
                Teacher = teacher,
                Supervisor = supervisor,
                Year = year,
                ClassA = classA,
                ClassB = classB,
                SubjectA = subjectA,
                SubjectB = subjectB,
                StudentA = studentA,
                StudentB = studentB,
                OutcomeA = outcomeA,
                OutcomeB = outcomeB,
                Users = users,
                Analytics = analytics,
                ExportRepository =
                    exportRepository,
                Audit = audit,
                Reports = reports,
                Exports = exports
            };
        }

        private static LearningOutcome
            NewOutcome(
                Guid schoolId,
                Guid subjectId,
                string code) =>
            new()
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                FrameworkVersionId =
                    Guid.NewGuid(),
                SubjectId = subjectId,
                GradeLevelId =
                    Guid.NewGuid(),
                TopicId =
                    Guid.NewGuid(),
                Code = code,
                Description =
                    code + " outcome",
                Weight = 1m,
                Order = 1
            };

        private static StudentOutcomeMastery
            NewMastery(
                Guid schoolId,
                Guid yearId,
                Guid classId,
                Guid subjectId,
                Guid studentId,
                Guid outcomeId,
                DateTime now) =>
            new()
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = yearId,
                ClassGroupId = classId,
                SubjectId = subjectId,
                StudentProfileId =
                    studentId,
                LearningOutcomeId =
                    outcomeId,
                EarnedScore = 8m,
                PossibleScore = 10m,
                MasteryPercentage = 80m,
                EvidenceCount = 1,
                Band = MasteryBand.Secure,
                CalculatedAtUtc = now
            };
    }

    private sealed class FakeAnalyticsRepository
        : IAnalyticsRepository
    {
        public MutableProjection Projection { get; }

        public FakeAnalyticsRepository(
            AnalyticsProjectionSnapshot projection)
        {
            Projection =
                new MutableProjection(
                    projection);
        }

        public Task<AnalyticsSourceSnapshot>
            GetSourceSnapshotAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new AnalyticsSourceSnapshot(
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    []));

        public Task<AnalyticsProjectionSnapshot>
            GetProjectionSnapshotAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Projection.ToSnapshot());

        public Task<DateTime?>
            GetLatestSourceUpdateAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTime?>(
                null);

        public Task<AnalyticsPersistenceResult>
            ReplaceProjectionsAsync(
                Guid schoolId,
                AnalyticsProjectionSet projections,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                AnalyticsPersistenceResult.Success());
    }

    private sealed class MutableProjection
    {
        public List<AcademicYear> AcademicYears { get; }
        public List<ClassGroup> ClassGroups { get; }
        public List<Subject> Subjects { get; }
        public List<StudentProfile> StudentProfiles { get; }
        public List<TeacherAssignment> TeacherAssignments { get; }
        public List<CurriculumTopic> CurriculumTopics { get; }
        public List<LearningOutcome> LearningOutcomes { get; }
        public List<StudentOutcomeMastery> StudentOutcomeMasteries { get; }
        public List<ClassOutcomeSummary> ClassOutcomeSummaries { get; }
        public List<ClassTopicSummary> ClassTopicSummaries { get; }
        public List<ClassAssessmentTrend> ClassAssessmentTrends { get; }
        public List<SchoolAnalyticsSnapshot> SchoolSnapshots { get; }

        public MutableProjection(
            AnalyticsProjectionSnapshot value)
        {
            AcademicYears =
                value.AcademicYears.ToList();

            ClassGroups =
                value.ClassGroups.ToList();

            Subjects =
                value.Subjects.ToList();

            StudentProfiles =
                value.StudentProfiles.ToList();

            TeacherAssignments =
                value.TeacherAssignments.ToList();

            CurriculumTopics =
                value.CurriculumTopics.ToList();

            LearningOutcomes =
                value.LearningOutcomes.ToList();

            StudentOutcomeMasteries =
                value.StudentOutcomeMasteries
                    .ToList();

            ClassOutcomeSummaries =
                value.ClassOutcomeSummaries
                    .ToList();

            ClassTopicSummaries =
                value.ClassTopicSummaries
                    .ToList();

            ClassAssessmentTrends =
                value.ClassAssessmentTrends
                    .ToList();

            SchoolSnapshots =
                value.SchoolSnapshots.ToList();
        }

        public AnalyticsProjectionSnapshot
            ToSnapshot() =>
            new(
                AcademicYears,
                ClassGroups,
                Subjects,
                StudentProfiles,
                TeacherAssignments,
                CurriculumTopics,
                LearningOutcomes,
                StudentOutcomeMasteries,
                ClassOutcomeSummaries,
                ClassTopicSummaries,
                ClassAssessmentTrends,
                SchoolSnapshots);
    }

    private sealed class FakeAssignmentRepository
        : ISubjectSupervisorAssignmentRepository
    {
        public List<SubjectSupervisorAssignment>
            Assignments { get; } = [];

        public List<Subject>
            Subjects { get; } = [];

        public Task<IReadOnlyList<
            SubjectSupervisorAssignment>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<
                SubjectSupervisorAssignment>>(
                Assignments
                    .Where(
                        x =>
                            x.SchoolId ==
                            schoolId)
                    .ToArray());

        public Task<IReadOnlyList<
            SubjectSupervisorAssignment>>
            ListActiveBySupervisorAsync(
                Guid schoolId,
                Guid supervisorUserId,
                CancellationToken cancellationToken = default)
        {
            var active =
                Subjects
                    .Where(
                        x =>
                            x.SchoolId ==
                                schoolId &&
                            x.Status ==
                                AcademicStructureStatus.Active)
                    .Select(x => x.Id)
                    .ToHashSet();

            return Task.FromResult<
                IReadOnlyList<
                    SubjectSupervisorAssignment>>(
                Assignments
                    .Where(
                        x =>
                            x.SchoolId ==
                                schoolId &&
                            x.SupervisorUserId ==
                                supervisorUserId &&
                            active.Contains(
                                x.SubjectId))
                    .ToArray());
        }

        public Task<IReadOnlyList<Subject>>
            ListSubjectsAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<Subject>>(
                Subjects
                    .Where(
                        x =>
                            x.SchoolId ==
                            schoolId)
                    .ToArray());

        public Task<Subject?> GetSubjectAsync(
            Guid schoolId,
            Guid subjectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Subjects.SingleOrDefault(
                    x =>
                        x.SchoolId ==
                            schoolId &&
                        x.Id ==
                            subjectId));

        public Task<SubjectSupervisorAssignment?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid assignmentId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Assignments.SingleOrDefault(
                    x =>
                        x.SchoolId ==
                            schoolId &&
                        x.Id ==
                            assignmentId));

        public Task<bool> ExistsAsync(
            Guid schoolId,
            Guid supervisorUserId,
            Guid subjectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Assignments.Any(
                    x =>
                        x.SchoolId ==
                            schoolId &&
                        x.SupervisorUserId ==
                            supervisorUserId &&
                        x.SubjectId ==
                            subjectId));

        public Task AddAsync(
            SubjectSupervisorAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            Assignments.Add(
                assignment);

            return Task.CompletedTask;
        }

        public void Remove(
            SubjectSupervisorAssignment assignment) =>
            Assignments.Remove(
                assignment);

        public Task<bool> SaveAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeUserRepository
        : ISchoolUserRepository
    {
        private readonly Dictionary<
            Guid,
            SchoolUserRecord> _users =
            [];

        public void Seed(
            SchoolUserRecord user) =>
            _users[user.Id] = user;

        public Task<SchoolUserRecord?>
            GetActorAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _users.GetValueOrDefault(
                    userId));

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<
                    SchoolUserRecord>>(
                _users.Values
                    .Where(
                        x =>
                            x.SchoolId ==
                            schoolId)
                    .ToArray());

        public Task<SchoolUserRecord?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                _users.GetValueOrDefault(
                    userId);

            return Task.FromResult(
                user?.SchoolId ==
                    schoolId
                    ? user
                    : null);
        }

        public Task<SchoolUserPersistenceResult>
            CreateAsync(
                Guid schoolId,
                string email,
                string role,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetActiveAsync(
                Guid schoolId,
                Guid userId,
                bool isActive,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetLockedAsync(
                Guid schoolId,
                Guid userId,
                bool isLocked,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetRoleAsync(
                Guid schoolId,
                Guid userId,
                string role,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            GeneratePasswordSetupAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            CompletePasswordSetupAsync(
                Guid userId,
                string token,
                string newPassword,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        private static Task<
            SchoolUserPersistenceResult>
            Unsupported() =>
            Task.FromResult(
                SchoolUserPersistenceResult
                    .Failure(
                        SchoolUserPersistenceError
                            .NotFound));
    }

    private sealed class FakeSchoolRepository
        : ISchoolRepository
    {
        private readonly Dictionary<
            Guid,
            School> _schools =
            [];

        public void Seed(
            School school) =>
            _schools[school.Id] =
                school;

        public Task<IReadOnlyList<School>>
            ListAsync(
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<School>>(
                _schools.Values.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.GetValueOrDefault(
                    id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(
                id,
                cancellationToken);

        public Task<bool>
            ExistsByNormalizedCodeAsync(
                string normalizedSchoolCode,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            Seed(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult>
            SaveAsync(
                School school,
                byte[]? expectedRowVersion,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolRepositoryWriteResult
                    .Success);
    }

    private sealed class FakeExportRepository
        : IReportExportRepository
    {
        public List<ReportExportJob>
            Jobs { get; } = [];

        public List<OutboxMessage>
            Outbox { get; } = [];

        public Task AddAsync(
            ReportExportJob job,
            CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task AddOutboxAsync(
            OutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            Outbox.Add(message);
            return Task.CompletedTask;
        }

        public Task<ReportExportJob?>
            GetAsync(
                Guid schoolId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Jobs.SingleOrDefault(
                    x =>
                        x.SchoolId ==
                            schoolId &&
                        x.Id ==
                            id));

        public Task<ReportExportJob?>
            GetForUpdateAsync(
                Guid schoolId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            GetAsync(
                schoolId,
                id,
                cancellationToken);

        public Task<IReadOnlyList<ReportExportJob>>
            ListRecentAsync(
                Guid schoolId,
                Guid requestedByUserId,
                int maxCount,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<
                    ReportExportJob>>(
                Jobs
                    .Where(
                        x =>
                            x.SchoolId ==
                                schoolId &&
                            x.RequestedByUserId ==
                                requestedByUserId)
                    .Take(maxCount)
                    .ToArray());

        public Task<bool> SaveAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeAuditService
        : IAuditService
    {
        public List<AuditEvent>
            Events { get; } = [];

        public Task QueueAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task RecordAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMetadataProvider
        : IAuditRequestMetadataProvider
    {
        public AuditRequestMetadata GetCurrent() =>
            new(
                null,
                RoleNames.SchoolAdmin,
                "phase20-test-correlation",
                "127.0.0.1",
                "Phase20Tests",
                "Tests");
    }
}
