using Edulytics.Core.Enums;
using Edulytics.Core.Lessons;
namespace Edulytics.Services.LessonContent;

public enum LessonContentErrorCode { AccessDenied=1, SchoolNotActive=2, LessonNotFound=3 }

public sealed record LessonContentQueryResult<T>(T? Value,LessonContentErrorCode? Error) where T:class
{
    public static LessonContentQueryResult<T> Success(T value)=>new(value,null);
    public static LessonContentQueryResult<T> Failure(LessonContentErrorCode error)=>new(null,error);
}

public sealed record CanonicalLessonLibraryItem(
    Guid LessonId,string LessonCode,string LessonTitle,string UnitTitle,int SortOrder,
    CanonicalLessonContentStatus? Status,DateTime? PublishedAtUtc,bool HasOfficialAlignment);

public sealed record CanonicalCurriculumLibraryGroup(
    Guid FrameworkVersionId,string FrameworkName,string FrameworkVersionName,string SubjectName,string SubjectCode,
    string GradeName,int TotalLessons,int ProductionReadyLessons,IReadOnlyList<CanonicalLessonLibraryItem> Lessons);

public sealed record LessonContentDashboard(Guid SchoolId,IReadOnlyList<CanonicalCurriculumLibraryGroup> Curricula);

public sealed record CanonicalLessonDetail(
    Guid LessonId,string LessonCode,string LessonTitle,string UnitTitle,string FrameworkName,string FrameworkVersionName,
    string SubjectName,string SubjectCode,string GradeName,CanonicalLessonContentStatus? Status,DateTime? PublishedAtUtc,
    CanonicalLessonTranslationRecord? Body,IReadOnlyList<LessonOutcomeRecord> Outcomes);

public sealed record StudentLessonSummary(
    Guid Id,string Title,string TopicName,string SubjectName,string SubjectCode,string GradeName,string FrameworkName,int Order);

public sealed record StudentLessonDetail(
    Guid Id,string Title,string TopicName,string SubjectName,string SubjectCode,string GradeName,string FrameworkName,
    string Explanation,string KeyConceptsAndRules,string WorkedExamples,string StepByStepSolutions,string CommonMistakes,
    string QuickSummary,IReadOnlyList<LessonOutcomeRecord> Outcomes,DateTime PublishedAtUtc);
