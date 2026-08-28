using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Core.Users;
namespace Edulytics.Services.LessonContent;

public sealed class LessonContentService : ILessonContentService
{
    private readonly ILessonContentRepository _lessons;
    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;

    public LessonContentService(
        ILessonContentRepository lessons,ISchoolUserRepository users,ISchoolRepository schools)
    { _lessons=lessons;_users=users;_schools=schools; }

    public async Task<LessonContentQueryResult<LessonContentDashboard>> GetDashboardAsync(
        Guid actorUserId,CancellationToken cancellationToken=default)
    {
        var scope=await ResolveScopeAsync(actorUserId,cancellationToken);
        if(!scope.Succeeded)return LessonContentQueryResult<LessonContentDashboard>.Failure(scope.Error!.Value);
        if(!LessonContentPolicy.CanReadStaff(scope.Actor!.Roles))
            return LessonContentQueryResult<LessonContentDashboard>.Failure(LessonContentErrorCode.AccessDenied);

        var contexts=await _lessons.ListStaffAdoptionsAsync(scope.School!.Id,cancellationToken);
        var lessons=await _lessons.ListPedagogicalLessonsAsync(
            contexts.Select(x=>x.FrameworkVersionId).Distinct().ToArray(),cancellationToken);
        var contents=await _lessons.ListCanonicalContentsAsync(
            lessons.Select(x=>x.Id).ToArray(),cancellationToken);
        var contentByLesson=contents.ToDictionary(x=>x.PedagogicalLessonId);

        var groups=contexts
            .GroupBy(x=>new{x.FrameworkVersionId,x.FrameworkCode,x.FrameworkName,x.FrameworkVersionName,
                x.SubjectName,x.SubjectCode,x.GradeName,x.GradeOrder})
            .Select(group=>
            {
                var c=group.First();
                var logicalLevel=ResolveLogicalLevel(c);
                var items=lessons
                    .Where(x=>x.FrameworkVersionId==c.FrameworkVersionId&&InLogicalLevel(x,logicalLevel))
                    .OrderBy(x=>x.SortOrder)
                    .Select(lesson=>
                    {
                        contentByLesson.TryGetValue(lesson.Id,out var content);
                        return new CanonicalLessonLibraryItem(
                            lesson.Id,lesson.Code,lesson.Title,lesson.UnitTitle,lesson.SortOrder,
                            content?.Status,content?.PublishedAtUtc,lesson.OfficialOutcomeCount>0);
                    }).ToArray();

                return new CanonicalCurriculumLibraryGroup(
                    c.FrameworkVersionId,c.FrameworkName,c.FrameworkVersionName,
                    c.SubjectName,c.SubjectCode,c.GradeName,
                    items.Length,items.Count(x=>x.Status==CanonicalLessonContentStatus.Published&&x.HasOfficialAlignment),items);
            })
            .OrderBy(x=>x.SubjectCode).ThenBy(x=>x.GradeName).ThenBy(x=>x.FrameworkName)
            .ToArray();

        return LessonContentQueryResult<LessonContentDashboard>.Success(
            new LessonContentDashboard(scope.School.Id,groups));
    }

    public async Task<LessonContentQueryResult<CanonicalLessonDetail>> GetStaffLessonAsync(
        Guid actorUserId,Guid lessonId,string cultureCode,CancellationToken cancellationToken=default)
    {
        var scope=await ResolveScopeAsync(actorUserId,cancellationToken);
        if(!scope.Succeeded)return LessonContentQueryResult<CanonicalLessonDetail>.Failure(scope.Error!.Value);
        if(!LessonContentPolicy.CanReadStaff(scope.Actor!.Roles))
            return LessonContentQueryResult<CanonicalLessonDetail>.Failure(LessonContentErrorCode.AccessDenied);
        var contexts=await _lessons.ListStaffAdoptionsAsync(scope.School!.Id,cancellationToken);
        return await BuildStaffDetailAsync(contexts,lessonId,cultureCode,cancellationToken);
    }

    public async Task<LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>> ListPublishedForStudentAsync(
        Guid actorUserId,string cultureCode,CancellationToken cancellationToken=default)
    {
        var scope=await ResolveScopeAsync(actorUserId,cancellationToken);
        if(!scope.Succeeded)
            return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Failure(scope.Error!.Value);
        if(!LessonContentPolicy.IsStudent(scope.Actor!.Roles))
            return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Failure(LessonContentErrorCode.AccessDenied);

        var contexts=await _lessons.ListStudentAdoptionsAsync(actorUserId,scope.School!.Id,cancellationToken);
        var lessons=await _lessons.ListPedagogicalLessonsAsync(
            contexts.Select(x=>x.FrameworkVersionId).Distinct().ToArray(),cancellationToken);
        var contents=await _lessons.ListCanonicalContentsAsync(
            lessons.Select(x=>x.Id).ToArray(),cancellationToken);
        var contentByLesson=contents
            .Where(x=>x.Status==CanonicalLessonContentStatus.Published)
            .ToDictionary(x=>x.PedagogicalLessonId);

        var result=new Dictionary<Guid,StudentLessonSummary>();
        foreach(var c in contexts)
        {
            var logicalLevel=ResolveLogicalLevel(c);
            foreach(var lesson in lessons.Where(x=>
                x.FrameworkVersionId==c.FrameworkVersionId&&
                InLogicalLevel(x,logicalLevel)&&
                x.OfficialOutcomeCount>0))
            {
                if(!contentByLesson.TryGetValue(lesson.Id,out var content))continue;
                var tr=SelectTranslation(content.Translations,cultureCode);
                if(tr is null)continue;

                result.TryAdd(lesson.Id,new StudentLessonSummary(
                    lesson.Id,tr.Title,lesson.UnitTitle,
                    c.SubjectName,c.SubjectCode,c.GradeName,c.FrameworkName,lesson.SortOrder));
            }
        }

        return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Success(
            result.Values.OrderBy(x=>x.SubjectCode).ThenBy(x=>x.Order).ToArray());
    }

    public async Task<LessonContentQueryResult<StudentLessonDetail>> GetPublishedForStudentAsync(
        Guid actorUserId,Guid lessonId,string cultureCode,CancellationToken cancellationToken=default)
    {
        var scope=await ResolveScopeAsync(actorUserId,cancellationToken);
        if(!scope.Succeeded)return LessonContentQueryResult<StudentLessonDetail>.Failure(scope.Error!.Value);
        if(!LessonContentPolicy.IsStudent(scope.Actor!.Roles))
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.AccessDenied);

        var contexts=await _lessons.ListStudentAdoptionsAsync(actorUserId,scope.School!.Id,cancellationToken);
        var lessons=await _lessons.ListPedagogicalLessonsAsync(
            contexts.Select(x=>x.FrameworkVersionId).Distinct().ToArray(),cancellationToken);
        var lesson=lessons.SingleOrDefault(x=>x.Id==lessonId);
        if(lesson is null||lesson.OfficialOutcomeCount==0)
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var c=contexts.FirstOrDefault(x=>
            x.FrameworkVersionId==lesson.FrameworkVersionId&&
            InLogicalLevel(lesson,ResolveLogicalLevel(x)));
        if(c is null)return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var content=(await _lessons.ListCanonicalContentsAsync([lessonId],cancellationToken)).SingleOrDefault();
        if(content is null||!LessonContentPolicy.CanExposeCanonicalBody(content.Status))
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var tr=SelectTranslation(content.Translations,cultureCode);
        if(tr is null)return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var outcomes=await _lessons.ListOfficialOutcomesAsync(
            lesson.FrameworkVersionId,lesson.Id,cancellationToken);

        return LessonContentQueryResult<StudentLessonDetail>.Success(new StudentLessonDetail(
            lesson.Id,tr.Title,lesson.UnitTitle,c.SubjectName,c.SubjectCode,c.GradeName,c.FrameworkName,
            tr.Explanation,tr.KeyConceptsAndRules,tr.WorkedExamples,tr.StepByStepSolutions,
            tr.CommonMistakes,tr.QuickSummary,outcomes,content.PublishedAtUtc??content.UpdatedAtUtc));
    }

    private async Task<LessonContentQueryResult<CanonicalLessonDetail>> BuildStaffDetailAsync(
        IReadOnlyList<CanonicalCurriculumContextRecord> contexts,Guid lessonId,
        string cultureCode,CancellationToken cancellationToken)
    {
        var lessons=await _lessons.ListPedagogicalLessonsAsync(
            contexts.Select(x=>x.FrameworkVersionId).Distinct().ToArray(),cancellationToken);
        var lesson=lessons.SingleOrDefault(x=>x.Id==lessonId);
        if(lesson is null)return LessonContentQueryResult<CanonicalLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var c=contexts.FirstOrDefault(x=>
            x.FrameworkVersionId==lesson.FrameworkVersionId&&
            InLogicalLevel(lesson,ResolveLogicalLevel(x)));
        if(c is null)return LessonContentQueryResult<CanonicalLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var content=(await _lessons.ListCanonicalContentsAsync([lessonId],cancellationToken)).SingleOrDefault();
        CanonicalLessonTranslationRecord? body=null;
        if(content is not null&&LessonContentPolicy.CanExposeCanonicalBody(content.Status))
            body=SelectTranslation(content.Translations,cultureCode);

        var outcomes=await _lessons.ListOfficialOutcomesAsync(
            lesson.FrameworkVersionId,lesson.Id,cancellationToken);

        return LessonContentQueryResult<CanonicalLessonDetail>.Success(new CanonicalLessonDetail(
            lesson.Id,lesson.Code,lesson.Title,lesson.UnitTitle,c.FrameworkName,c.FrameworkVersionName,
            c.SubjectName,c.SubjectCode,c.GradeName,content?.Status,content?.PublishedAtUtc,body,outcomes));
    }

    private async Task<ScopeResult> ResolveScopeAsync(Guid actorUserId,CancellationToken cancellationToken)
    {
        var actor=await _users.GetActorAsync(actorUserId,cancellationToken);
        if(actor is null||!actor.IsActive||actor.IsLocked||!actor.SchoolId.HasValue)
            return ScopeResult.Fail(LessonContentErrorCode.AccessDenied);
        var school=await _schools.GetByIdAsync(actor.SchoolId.Value,cancellationToken);
        if(school is null||school.Status!=SchoolStatus.Active)
            return ScopeResult.Fail(LessonContentErrorCode.SchoolNotActive);
        return ScopeResult.Success(actor,school);
    }

    private static bool InLogicalLevel(PedagogicalLessonRecord lesson,int logicalLevel)=>
        lesson.LogicalLevelFrom<=logicalLevel&&logicalLevel<=lesson.LogicalLevelTo;

    // Must match CurriculumService's verified native-grade mapping logic.
    private static int ResolveLogicalLevel(CanonicalCurriculumContextRecord context)
    {
        var pack=MathematicsCurriculumPackRegistry.All.Single(x=>
            string.Equals(x.Code,context.FrameworkCode,StringComparison.Ordinal));

        var exact=pack.Levels.FirstOrDefault(x=>
            string.Equals(x.NativeLabel,context.GradeName,StringComparison.OrdinalIgnoreCase));
        if(exact is not null)return exact.LogicalLevel;

        var gradeNumberMatch=Regex.Match(
            context.GradeName ?? string.Empty,
            @"\d+",
            RegexOptions.CultureInvariant);

        if(gradeNumberMatch.Success&&
           int.TryParse(gradeNumberMatch.Value,out var gradeNumber))
        {
            var native=pack.Levels.FirstOrDefault(x=>
                string.Equals(x.NativeLabel,$"Grade {gradeNumber}",StringComparison.OrdinalIgnoreCase)||
                string.Equals(x.NativeLabel,$"Year {gradeNumber}",StringComparison.OrdinalIgnoreCase));
            if(native is not null)return native.LogicalLevel;
        }

        return context.GradeOrder;
    }

    private static CanonicalLessonTranslationRecord? SelectTranslation(
        IReadOnlyList<CanonicalLessonTranslationRecord> translations,string cultureCode)
    {
        var c=NormalizeCulture(cultureCode);
        return translations.FirstOrDefault(x=>NormalizeCulture(x.CultureCode)==c)
            ??translations.FirstOrDefault(x=>NormalizeCulture(x.CultureCode)=="en");
    }

    private static string NormalizeCulture(string cultureCode)
    {
        if(string.IsNullOrWhiteSpace(cultureCode))return "en";
        var v=cultureCode.Trim();var i=v.IndexOf('-');
        return (i>0?v[..i]:v).ToLowerInvariant();
    }

    private sealed record ScopeResult(SchoolUserRecord? Actor,School? School,LessonContentErrorCode? Error)
    {
        public bool Succeeded=>Actor is not null&&School is not null&&Error is null;
        public static ScopeResult Success(SchoolUserRecord actor,School school)=>new(actor,school,null);
        public static ScopeResult Fail(LessonContentErrorCode error)=>new(null,null,error);
    }
}
