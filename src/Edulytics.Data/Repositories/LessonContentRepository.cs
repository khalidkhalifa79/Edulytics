using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
namespace Edulytics.Data.Repositories;

public sealed class LessonContentRepository : ILessonContentRepository
{
    private const string LessonStandardAlignment = "LessonStandardAlignment";
    private readonly EdulyticsDbContext _db;
    public LessonContentRepository(EdulyticsDbContext db)=>_db=db;

    public async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStaffAdoptionsAsync(Guid schoolId,CancellationToken cancellationToken=default)
    {
        var adoptions=await _db.SchoolCurriculumAdoptions.AsNoTracking()
            .Where(x=>x.SchoolId==schoolId&&x.IsActive&&x.IsPrimary).ToArrayAsync(cancellationToken);
        return await HydrateContextsAsync(schoolId,adoptions,cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStudentAdoptionsAsync(Guid actorUserId,Guid schoolId,CancellationToken cancellationToken=default)
    {
        var profile=await _db.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(
            x=>x.SchoolId==schoolId&&x.UserId==actorUserId&&!x.IsArchived&&x.Status==AcademicStructureStatus.Active,cancellationToken);
        if(profile is null)return [];
        var enrollments=await _db.StudentEnrollments.AsNoTracking()
            .Where(x=>x.SchoolId==schoolId&&x.StudentProfileId==profile.Id).ToArrayAsync(cancellationToken);
        if(enrollments.Length==0)return [];
        var classIds=enrollments.Select(x=>x.ClassGroupId).Distinct().ToArray();
        var yearIds=enrollments.Select(x=>x.AcademicYearId).Distinct().ToArray();
        var classes=await _db.ClassGroups.AsNoTracking().Where(x=>x.SchoolId==schoolId&&classIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        var gradeIds=classes.Select(x=>x.GradeLevelId).Distinct().ToArray();
        if(gradeIds.Length==0)return [];
        var adoptions=await _db.SchoolCurriculumAdoptions.AsNoTracking()
            .Where(x=>x.SchoolId==schoolId&&x.IsActive&&x.IsPrimary&&gradeIds.Contains(x.GradeLevelId)&&
                (!x.AcademicYearId.HasValue||yearIds.Contains(x.AcademicYearId.Value))).ToArrayAsync(cancellationToken);
        return await HydrateContextsAsync(schoolId,adoptions,cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalCurriculumNodeRecord>> ListCurriculumNodesAsync(IReadOnlyCollection<Guid> frameworkVersionIds,CancellationToken cancellationToken=default)
    {
        if(frameworkVersionIds.Count==0)return [];
        var ids=frameworkVersionIds.Distinct().ToArray();
        return await _db.CurriculumPackContentNodes.AsNoTracking()
            .Where(x=>ids.Contains(x.FrameworkVersionId)&&x.IsActive&&(x.NodeKind=="Unit"||x.NodeKind=="Lesson"))
            .OrderBy(x=>x.FrameworkVersionId).ThenBy(x=>x.SortOrder)
            .Select(x=>new CanonicalCurriculumNodeRecord(x.Id,x.FrameworkVersionId,x.ParentId,x.NodeKind,x.Code,x.Title,
                x.OfficialText,x.Pathway,x.LogicalLevelFrom,x.LogicalLevelTo,x.SortOrder)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalLessonContentRecord>> ListCanonicalContentsAsync(IReadOnlyCollection<Guid> lessonNodeIds,CancellationToken cancellationToken=default)
    {
        if(lessonNodeIds.Count==0)return [];
        var nodeIds=lessonNodeIds.Distinct().ToArray();
        var contents=await _db.CurriculumLessonContents.AsNoTracking().Where(x=>nodeIds.Contains(x.LessonNodeId)).ToArrayAsync(cancellationToken);
        if(contents.Length==0)return [];
        var contentIds=contents.Select(x=>x.Id).ToArray();
        var translations=await _db.CurriculumLessonContentTranslations.AsNoTracking()
            .Where(x=>contentIds.Contains(x.CurriculumLessonContentId)).OrderBy(x=>x.CultureCode).ToArrayAsync(cancellationToken);
        return contents.Select(c=>new CanonicalLessonContentRecord(c.Id,c.FrameworkVersionId,c.LessonNodeId,c.Status,c.ContentVersion,
            c.VerifiedAtUtc,c.PublishedAtUtc,c.UpdatedAtUtc,
            translations.Where(t=>t.CurriculumLessonContentId==c.Id)
                .Select(t=>new CanonicalLessonTranslationRecord(t.CultureCode,t.Title,t.Explanation,t.KeyConceptsAndRules,
                    t.WorkedExamples,t.StepByStepSolutions,t.CommonMistakes,t.QuickSummary)).ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<LessonOutcomeRecord>> ListOfficialOutcomesAsync(Guid frameworkVersionId,Guid lessonNodeId,CancellationToken cancellationToken=default)
    {
        var links=await _db.CurriculumPackNodeLinks.AsNoTracking()
            .Where(x=>x.FrameworkVersionId==frameworkVersionId&&x.LinkKind==LessonStandardAlignment&&
                (x.FromNodeId==lessonNodeId||x.ToNodeId==lessonNodeId)).OrderBy(x=>x.SortOrder).ToArrayAsync(cancellationToken);
        if(links.Length==0)return [];
        var otherIds=links.Select(x=>x.FromNodeId==lessonNodeId?x.ToNodeId:x.FromNodeId).Distinct().ToArray();
        var nodes=await _db.CurriculumPackContentNodes.AsNoTracking()
            .Where(x=>x.FrameworkVersionId==frameworkVersionId&&x.IsActive&&otherIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        var byId=nodes.ToDictionary(x=>x.Id);
        return links.Select((link,index)=>{
            var id=link.FromNodeId==lessonNodeId?link.ToNodeId:link.FromNodeId;
            if(!byId.TryGetValue(id,out var node))return null;
            return new LessonOutcomeRecord(node.Id,node.Code,node.OfficialText??node.AuthorDescription??node.Title,
                link.SortOrder!=0?link.SortOrder:index+1);
        }).Where(x=>x is not null).Select(x=>x!).GroupBy(x=>x.Id).Select(x=>x.First()).OrderBy(x=>x.Order).ToArray();
    }

    private async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> HydrateContextsAsync(
        Guid schoolId,IReadOnlyCollection<Edulytics.Core.Entities.SchoolCurriculumAdoption> adoptions,CancellationToken cancellationToken)
    {
        if(adoptions.Count==0)return [];
        var subjectIds=adoptions.Select(x=>x.SubjectId).Distinct().ToArray();
        var gradeIds=adoptions.Select(x=>x.GradeLevelId).Distinct().ToArray();
        var versionIds=adoptions.Select(x=>x.FrameworkVersionId).Distinct().ToArray();
        var subjects=await _db.Subjects.AsNoTracking().Where(x=>x.SchoolId==schoolId&&subjectIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        var grades=await _db.GradeLevels.AsNoTracking().Where(x=>x.SchoolId==schoolId&&gradeIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        var versions=await _db.CurriculumFrameworkVersions.AsNoTracking().Where(x=>versionIds.Contains(x.Id)&&x.IsActive).ToArrayAsync(cancellationToken);
        var frameworkIds=versions.Select(x=>x.FrameworkId).Distinct().ToArray();
        var frameworks=await _db.CurriculumFrameworks.AsNoTracking().Where(x=>frameworkIds.Contains(x.Id)&&x.IsActive).ToArrayAsync(cancellationToken);
        var sb=subjects.ToDictionary(x=>x.Id);var gb=grades.ToDictionary(x=>x.Id);var vb=versions.ToDictionary(x=>x.Id);var fb=frameworks.ToDictionary(x=>x.Id);
        var result=new List<CanonicalCurriculumContextRecord>();
        foreach(var a in adoptions)
        {
            if(!sb.TryGetValue(a.SubjectId,out var subject)||!gb.TryGetValue(a.GradeLevelId,out var grade)||
               !vb.TryGetValue(a.FrameworkVersionId,out var version)||!fb.TryGetValue(version.FrameworkId,out var framework))continue;
            result.Add(new CanonicalCurriculumContextRecord(version.Id,framework.Name,version.Name,subject.Id,subject.Name,subject.Code,
                grade.Id,grade.Name,grade.Order));
        }
        return result.Distinct().OrderBy(x=>x.SubjectCode).ThenBy(x=>x.GradeOrder).ThenBy(x=>x.FrameworkName).ToArray();
    }
}
