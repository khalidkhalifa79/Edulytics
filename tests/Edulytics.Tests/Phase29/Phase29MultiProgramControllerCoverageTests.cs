using System.Security.Claims;
using Edulytics.Core.Enums;
using Edulytics.Services.Academics;
using Edulytics.Services.Curriculum;
using Edulytics.Web;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29MultiProgramControllerCoverageTests
{
    [Fact]
    public async Task AcademicController_CoversAuthenticatedSuccessFailureAndEditFlows()
    {
        var actorId = Guid.NewGuid();
        var service = new FakeAcademicService();
        var controller = Prepare(
            new AcademicStructureController(
                service,
                new EchoLocalizer<AcademicResource>()),
            actorId);

        Assert.IsType<ViewResult>(
            await controller.Index(CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateAcademicYear(
                "2027/2028",
                new DateOnly(2027, 9, 1),
                new DateOnly(2028, 6, 30),
                AcademicStructureStatus.Active,
                CancellationToken.None));

        Assert.IsType<ViewResult>(
            await controller.EditAcademicYear(
                service.Year.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditAcademicYear(
                service.Year.Id,
                "2027/2028",
                service.Year.StartsOn,
                service.Year.EndsOn,
                AcademicStructureStatus.Active,
                "not-base64",
                CancellationToken.None));

        var rowVersion = Convert.ToBase64String([1, 2, 3, 4]);

        Assert.IsType<RedirectToActionResult>(
            await controller.EditAcademicYear(
                service.Year.Id,
                "2027/2028",
                service.Year.StartsOn,
                service.Year.EndsOn,
                AcademicStructureStatus.Active,
                rowVersion,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateTerm(
                service.Year.Id,
                "Term 1",
                service.Year.StartsOn,
                service.Year.StartsOn.AddMonths(3),
                AcademicStructureStatus.Active,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateGradeLevel(
                "Grade 7",
                7,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateAcademicProgram(
                service.Year.Id,
                "british",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateClassGroup(
                service.Year.Id,
                service.Program.Id,
                service.Grade.Id,
                "6A",
                AcademicStructureStatus.Active,
                CancellationToken.None));

        Assert.IsType<ViewResult>(
            await controller.EditClassGroup(
                service.ClassGroup.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditClassGroup(
                service.ClassGroup.Id,
                service.Program.Id,
                service.Grade.Id,
                "6A",
                AcademicStructureStatus.Active,
                "bad",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditClassGroup(
                service.ClassGroup.Id,
                service.Program.Id,
                service.Grade.Id,
                "6A",
                AcademicStructureStatus.Active,
                rowVersion,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateSubject(
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active,
                CancellationToken.None));

        Assert.IsType<ViewResult>(
            await controller.EditSubject(
                service.Subject.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditSubject(
                service.Subject.Id,
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active,
                "bad",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditSubject(
                service.Subject.Id,
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active,
                rowVersion,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateTeacherAssignment(
                Guid.NewGuid(),
                service.ClassGroup.Id,
                service.Subject.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateStudentProfile(
                "ST-100",
                "Ada",
                "Nowak",
                null,
                AcademicStructureStatus.Active,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.ArchiveStudentProfile(
                Guid.NewGuid(),
                "bad",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.ArchiveStudentProfile(
                Guid.NewGuid(),
                rowVersion,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.RestoreStudentProfile(
                Guid.NewGuid(),
                "bad",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.RestoreStudentProfile(
                Guid.NewGuid(),
                rowVersion,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateStudentEnrollment(
                Guid.NewGuid(),
                service.ClassGroup.Id,
                CancellationToken.None));

        service.CommandResult = AcademicCommandResult.Failure(
            "x",
            AcademicStructureErrorCode.PersistenceError);

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateAcademicProgram(
                service.Year.Id,
                "not-a-valid-program-choice",
                CancellationToken.None));

        service.YearResult =
            AcademicQueryResult<AcademicYearItem>.Failure(
                AcademicStructureErrorCode.AcademicYearNotFound);

        Assert.IsType<NotFoundResult>(
            await controller.EditAcademicYear(
                Guid.NewGuid(),
                CancellationToken.None));

        service.ClassResult =
            AcademicQueryResult<ClassGroupItem>.Failure(
                AcademicStructureErrorCode.ClassGroupNotFound);

        Assert.IsType<NotFoundResult>(
            await controller.EditClassGroup(
                Guid.NewGuid(),
                CancellationToken.None));

        service.SubjectResult =
            AcademicQueryResult<SubjectItem>.Failure(
                AcademicStructureErrorCode.SubjectNotFound);

        Assert.IsType<NotFoundResult>(
            await controller.EditSubject(
                Guid.NewGuid(),
                CancellationToken.None));

        Prepare(controller, null);

        Assert.IsType<ForbidResult>(
            await controller.Index(CancellationToken.None));

        Assert.IsType<ForbidResult>(
            await controller.CreateGradeLevel(
                "Blocked",
                9,
                CancellationToken.None));
    }

    [Fact]
    public async Task CurriculumController_CoversQueriesFeedbackParsingAndEveryErrorKey()
    {
        var actorId = Guid.NewGuid();
        var service = new FakeCurriculumService();
        var controller = Prepare(
            new CurriculumController(
                service,
                new EchoLocalizer<CurriculumResource>()),
            actorId);

        Assert.IsType<ViewResult>(
            await controller.Index(CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.SelectFramework(
                service.Subject.Id,
                service.Grade.Id,
                service.Program.Id,
                "UK-NC-ENG-MATH",
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateTopic(
                service.Subject.Id,
                service.Grade.Id,
                service.Program.Id,
                "Numbers",
                1,
                CancellationToken.None));

        Assert.IsType<ViewResult>(
            await controller.EditTopic(
                service.Topic.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditTopic(
                service.Topic.Id,
                "Numbers updated",
                2,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateOfficialOutcome(
                service.Topic.Id,
                "invalid-selection",
                1,
                CancellationToken.None));

        var contentId = service.Topic.OfficialOutcomes[0].ContentNodeId;
        var lessonId = service.Topic.OfficialOutcomes[0].LessonNodeId!.Value;

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateOfficialOutcome(
                service.Topic.Id,
                contentId.ToString(),
                1,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateOfficialOutcome(
                service.Topic.Id,
                $"{contentId}|{lessonId}",
                1,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.CreateOfficialOutcome(
                service.Topic.Id,
                $"{contentId}|not-a-guid",
                1,
                CancellationToken.None));

        Assert.IsType<ViewResult>(
            await controller.EditOutcome(
                service.Outcome.Id,
                CancellationToken.None));

        Assert.IsType<RedirectToActionResult>(
            await controller.EditOutcome(
                service.Outcome.Id,
                service.Outcome.Code,
                service.Outcome.Description,
                2,
                CancellationToken.None));

        foreach (var code in Enum.GetValues<CurriculumErrorCode>())
        {
            service.CommandResult =
                CurriculumCommandResult.Failure("x", code);

            Assert.IsType<RedirectToActionResult>(
                await controller.CreateTopic(
                    service.Subject.Id,
                    service.Grade.Id,
                    service.Program.Id,
                    "Error branch",
                    99,
                    CancellationToken.None));
        }

        service.CommandResult = CurriculumCommandResult.Success();

        service.TopicResult =
            CurriculumQueryResult<CurriculumTopicItem>.Failure(
                CurriculumErrorCode.AccessDenied);

        Assert.IsType<ForbidResult>(
            await controller.EditTopic(
                Guid.NewGuid(),
                CancellationToken.None));

        service.TopicResult =
            CurriculumQueryResult<CurriculumTopicItem>.Failure(
                CurriculumErrorCode.TopicNotFound);

        Assert.IsType<NotFoundResult>(
            await controller.EditTopic(
                Guid.NewGuid(),
                CancellationToken.None));

        service.OutcomeResult =
            CurriculumQueryResult<LearningOutcomeItem>.Failure(
                CurriculumErrorCode.AccessDenied);

        Assert.IsType<ForbidResult>(
            await controller.EditOutcome(
                Guid.NewGuid(),
                CancellationToken.None));

        service.DashboardResult =
            CurriculumQueryResult<CurriculumDashboard>.Failure(
                CurriculumErrorCode.TopicNotFound);

        Assert.IsType<NotFoundResult>(
            await controller.Index(CancellationToken.None));

        service.DashboardResult =
            CurriculumQueryResult<CurriculumDashboard>.Failure(
                CurriculumErrorCode.AccessDenied);

        Assert.IsType<ForbidResult>(
            await controller.Index(CancellationToken.None));

        Prepare(controller, null);

        Assert.IsType<ForbidResult>(
            await controller.Index(CancellationToken.None));

        Assert.IsType<ForbidResult>(
            await controller.SelectFramework(
                service.Subject.Id,
                service.Grade.Id,
                service.Program.Id,
                "UK-NC-ENG-MATH",
                CancellationToken.None));
    }

    private static T Prepare<T>(T controller, Guid? actorId)
        where T : Controller
    {
        var http = new DefaultHttpContext();

        if (actorId.HasValue)
        {
            http.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(
                        ClaimTypes.NameIdentifier,
                        actorId.Value.ToString())],
                    "test"));
        }

        controller.ControllerContext =
            new ControllerContext { HttpContext = http };

        controller.TempData =
            new TempDataDictionary(
                http,
                new MemoryTempDataProvider());

        return controller;
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = [];

        public IDictionary<string, object> LoadTempData(
            HttpContext context) =>
            new Dictionary<string, object>(_values);

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values) =>
            _values = new Dictionary<string, object>(values);
    }

    private sealed class EchoLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] =>
            new(name, name, resourceNotFound: false);

        public LocalizedString this[
            string name,
            params object[] arguments] =>
            new(
                name,
                string.Format(name, arguments),
                resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(
            bool includeParentCultures) =>
            [];
    }

    private sealed class FakeAcademicService : IAcademicStructureService
    {
        public AcademicYearItem Year { get; } =
            new(
                Guid.NewGuid(),
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active,
                [1, 2, 3, 4]);

        public GradeLevelItem Grade { get; } =
            new(Guid.NewGuid(), "Grade 6", 6);

        public AcademicProgramItem Program { get; } =
            new(
                Guid.NewGuid(),
                "British Stream",
                "BRITISH",
                AcademicStructureStatus.Active,
                true,
                [1, 2, 3, 4]);

        public SubjectItem Subject { get; } =
            new(
                Guid.NewGuid(),
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active,
                [1, 2, 3, 4]);

        public ClassGroupItem ClassGroup { get; }

        public AcademicStructureDashboard Dashboard { get; }

        public AcademicCommandResult CommandResult { get; set; } =
            AcademicCommandResult.Success();

        public AcademicQueryResult<AcademicYearItem> YearResult { get; set; }
        public AcademicQueryResult<ClassGroupItem> ClassResult { get; set; }
        public AcademicQueryResult<SubjectItem> SubjectResult { get; set; }

        public FakeAcademicService()
        {
            ClassGroup =
                new ClassGroupItem(
                    Guid.NewGuid(),
                    Year.Id,
                    Year.Name,
                    Grade.Id,
                    Grade.Name,
                    "6A",
                    "6A",
                    AcademicStructureStatus.Active,
                    [1, 2, 3, 4])
                {
                    AcademicProgramId = Program.Id,
                    AcademicProgramName = Program.Name,
                    AcademicProgramCode = Program.Code
                };

            Dashboard =
                new AcademicStructureDashboard(
                    Guid.NewGuid(),
                    "Coverage School",
                    [Year],
                    [],
                    [Grade],
                    [ClassGroup],
                    [Subject],
                    [],
                    [],
                    [],
                    [],
                    [])
                {
                    AcademicPrograms = [Program]
                };

            YearResult =
                AcademicQueryResult<AcademicYearItem>.Success(Year);
            ClassResult =
                AcademicQueryResult<ClassGroupItem>.Success(ClassGroup);
            SubjectResult =
                AcademicQueryResult<SubjectItem>.Success(Subject);
        }

        public Task<AcademicQueryResult<AcademicStructureDashboard>>
            GetDashboardAsync(
                Guid actorUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                AcademicQueryResult<AcademicStructureDashboard>.Success(
                    Dashboard));

        public Task<AcademicQueryResult<AcademicYearItem>>
            GetAcademicYearAsync(
                Guid actorUserId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(YearResult);

        public Task<AcademicQueryResult<ClassGroupItem>>
            GetClassGroupAsync(
                Guid actorUserId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(ClassResult);

        public Task<AcademicQueryResult<SubjectItem>>
            GetSubjectAsync(
                Guid actorUserId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(SubjectResult);

        public Task<AcademicCommandResult> CreateAcademicYearAsync(
            Guid actorUserId,
            CreateAcademicYearRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> UpdateAcademicYearAsync(
            Guid actorUserId,
            UpdateAcademicYearRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateTermAsync(
            Guid actorUserId,
            CreateTermRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateGradeLevelAsync(
            Guid actorUserId,
            CreateGradeLevelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateAcademicProgramAsync(
            Guid actorUserId,
            CreateAcademicProgramRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateClassGroupAsync(
            Guid actorUserId,
            CreateClassGroupRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> UpdateClassGroupAsync(
            Guid actorUserId,
            UpdateClassGroupRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateSubjectAsync(
            Guid actorUserId,
            CreateSubjectRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> UpdateSubjectAsync(
            Guid actorUserId,
            UpdateSubjectRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateTeacherAssignmentAsync(
            Guid actorUserId,
            CreateTeacherAssignmentRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateStudentProfileAsync(
            Guid actorUserId,
            CreateStudentProfileRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> ArchiveStudentProfileAsync(
            Guid actorUserId,
            Guid studentProfileId,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> RestoreStudentProfileAsync(
            Guid actorUserId,
            Guid studentProfileId,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<AcademicCommandResult> CreateStudentEnrollmentAsync(
            Guid actorUserId,
            CreateStudentEnrollmentRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);
    }

    private sealed class FakeCurriculumService : ICurriculumService
    {
        public CurriculumProgramItem Program { get; } =
            new(Guid.NewGuid(), "British Stream", "BRITISH");

        public CurriculumGradeItem Grade { get; } =
            new(Guid.NewGuid(), "Grade 6", 6);

        public CurriculumSubjectItem Subject { get; } =
            new(Guid.NewGuid(), "Mathematics", "MATH");

        public LearningOutcomeItem Outcome { get; }

        public CurriculumTopicItem Topic { get; }

        public CurriculumDashboard Dashboard { get; }

        public CurriculumCommandResult CommandResult { get; set; } =
            CurriculumCommandResult.Success();

        public CurriculumQueryResult<CurriculumDashboard>
            DashboardResult
        { get; set; }

        public CurriculumQueryResult<CurriculumTopicItem>
            TopicResult
        { get; set; }

        public CurriculumQueryResult<LearningOutcomeItem>
            OutcomeResult
        { get; set; }

        public FakeCurriculumService()
        {
            Outcome =
                new LearningOutcomeItem(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "MATH.6.1",
                    "Add whole numbers.",
                    1m,
                    1)
                {
                    IsOfficial = true
                };

            var contentId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();

            Topic =
                new CurriculumTopicItem(
                    Outcome.TopicId,
                    Subject.Id,
                    Grade.Id,
                    "Numbers",
                    1,
                    [Outcome])
                {
                    AcademicProgramId = Program.Id,
                    AcademicProgramName = Program.Name,
                    FrameworkCode = "UK-NC-ENG-MATH",
                    FrameworkDisplayName = "British / UK Mathematics — England",
                    OfficialOutcomes =
                    [
                        new OfficialCurriculumOutcomeOption(
                            contentId,
                            lessonId,
                            "UK.6.N.1",
                            "Official option",
                            "Option",
                            "Number",
                            1)
                    ]
                };

            Dashboard =
                new CurriculumDashboard(
                    Guid.NewGuid(),
                    [Grade],
                    [Subject],
                    [Topic])
                {
                    AcademicPrograms = [Program],
                    Frameworks =
                    [
                        new CurriculumFrameworkItem(
                            "UK-NC-ENG-MATH",
                            "British / UK Mathematics — England")
                    ],
                    Adoptions =
                    [
                        new CurriculumAdoptionItem(
                            Grade.Id,
                            Subject.Id,
                            "UK-NC-ENG-MATH",
                            "British / UK Mathematics — England")
                        {
                            AcademicProgramId = Program.Id,
                            AcademicProgramName = Program.Name,
                            AcademicProgramCode = Program.Code
                        }
                    ]
                };

            DashboardResult =
                CurriculumQueryResult<CurriculumDashboard>.Success(
                    Dashboard);
            TopicResult =
                CurriculumQueryResult<CurriculumTopicItem>.Success(
                    Topic);
            OutcomeResult =
                CurriculumQueryResult<LearningOutcomeItem>.Success(
                    Outcome);
        }

        public Task<CurriculumQueryResult<CurriculumDashboard>>
            GetDashboardAsync(
                Guid actorUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(DashboardResult);

        public Task<CurriculumQueryResult<CurriculumTopicItem>>
            GetTopicAsync(
                Guid actorUserId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(TopicResult);

        public Task<CurriculumQueryResult<LearningOutcomeItem>>
            GetOutcomeAsync(
                Guid actorUserId,
                Guid id,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(OutcomeResult);

        public Task<CurriculumCommandResult> SelectFrameworkAsync(
            Guid actorUserId,
            SelectCurriculumFrameworkRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<CurriculumCommandResult> CreateTopicAsync(
            Guid actorUserId,
            CreateCurriculumTopicRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<CurriculumCommandResult> UpdateTopicAsync(
            Guid actorUserId,
            UpdateCurriculumTopicRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<CurriculumCommandResult> CreateOfficialOutcomeAsync(
            Guid actorUserId,
            CreateOfficialLearningOutcomeRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<CurriculumCommandResult> UpdateOutcomeAsync(
            Guid actorUserId,
            UpdateLearningOutcomeRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);
    }
}
