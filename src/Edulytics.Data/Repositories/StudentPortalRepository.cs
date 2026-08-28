using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.StudentPortal;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class StudentPortalRepository : IStudentPortalRepository
{
    private readonly EdulyticsDbContext _db;

    public StudentPortalRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<StudentPortalSnapshot> GetSnapshotAsync(
        Guid schoolId,
        Guid studentUserId,
        CancellationToken cancellationToken = default)
    {
        var profile =
            await _db.StudentProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.SchoolId == schoolId &&
                        x.UserId == studentUserId &&
                        !x.IsArchived &&
                        x.Status == AcademicStructureStatus.Active,
                    cancellationToken);

        if (profile is null)
        {
            return Empty();
        }

        var enrollments =
            await _db.StudentEnrollments
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        x.StudentProfileId == profile.Id)
                .OrderByDescending(x => x.EnrolledAtUtc)
                .ToArrayAsync(cancellationToken);

        var classIds =
            enrollments
                .Select(x => x.ClassGroupId)
                .Distinct()
                .ToArray();

        var yearIds =
            enrollments
                .Select(x => x.AcademicYearId)
                .Distinct()
                .ToArray();

        var classGroups =
            await _db.ClassGroups
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        classIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var gradeIds = classGroups.Select(x => x.GradeLevelId).Distinct().ToArray();
        var classById = classGroups.ToDictionary(x => x.Id);
        var scopes = enrollments.Where(x => classById.ContainsKey(x.ClassGroupId)).Select(x => new { classById[x.ClassGroupId].AcademicProgramId, classById[x.ClassGroupId].GradeLevelId, x.AcademicYearId }).Distinct().ToArray();
        var programIds = scopes.Select(x => x.AcademicProgramId).Distinct().ToArray();

        var academicYears =
            await _db.AcademicYears
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        yearIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var gradeLevels =
            await _db.GradeLevels
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        gradeIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var adoptionCandidates = await _db.SchoolCurriculumAdoptions.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive && x.IsPrimary && programIds.Contains(x.AcademicProgramId) && gradeIds.Contains(x.GradeLevelId) && (!x.AcademicYearId.HasValue || yearIds.Contains(x.AcademicYearId.Value))).ToArrayAsync(cancellationToken);
        var adoptions = adoptionCandidates.Where(a => scopes.Any(q => a.AcademicProgramId == q.AcademicProgramId && a.GradeLevelId == q.GradeLevelId && (!a.AcademicYearId.HasValue || a.AcademicYearId.Value == q.AcademicYearId))).ToArray();

        var assessments =
            await _db.Assessments
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        classIds.Contains(x.ClassGroupId) &&
                        yearIds.Contains(x.AcademicYearId) &&
                        x.Status != AssessmentStatus.Draft)
                .ToArrayAsync(cancellationToken);

        var subjectIds =
            adoptions
                .Select(x => x.SubjectId)
                .Concat(assessments.Select(x => x.SubjectId))
                .Distinct()
                .ToArray();

        var subjects =
            await _db.Subjects
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        subjectIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var frameworkVersionIds =
            adoptions
                .Select(x => x.FrameworkVersionId)
                .Distinct()
                .ToArray();

        var frameworkVersions =
            await _db.CurriculumFrameworkVersions
                .AsNoTracking()
                .Where(x => frameworkVersionIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var frameworkIds =
            frameworkVersions
                .Select(x => x.FrameworkId)
                .Distinct()
                .ToArray();

        var frameworks =
            await _db.CurriculumFrameworks
                .AsNoTracking()
                .Where(x => frameworkIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

        var curriculumNodes =
            await _db.CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        frameworkVersionIds.Contains(x.FrameworkVersionId) &&
                        x.IsActive &&
                        (x.NodeKind == "Unit" ||
                         x.NodeKind == "Lesson"))
                .OrderBy(x => x.SortOrder)
                .ToArrayAsync(cancellationToken);

        var results =
            await _db.AssessmentResults
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        x.StudentProfileId == profile.Id)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ToArrayAsync(cancellationToken);

        return new StudentPortalSnapshot(
            profile,
            enrollments,
            academicYears,
            classGroups,
            gradeLevels,
            subjects,
            adoptions,
            frameworks,
            frameworkVersions,
            curriculumNodes,
            assessments,
            results);
    }

    private static StudentPortalSnapshot Empty() =>
        new(
            null,
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
            []);
}
