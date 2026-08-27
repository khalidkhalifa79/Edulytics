using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Services.StudentPortal;

public sealed class StudentPortalService : IStudentPortalService
{
    private readonly IStudentPortalRepository _portal;
    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;

    public StudentPortalService(
        IStudentPortalRepository portal,
        ISchoolUserRepository users,
        ISchoolRepository schools)
    {
        _portal = portal;
        _users = users;
        _schools = schools;
    }

    public async Task<StudentPortalQueryResult<StudentPortalWorkspace>>
        GetWorkspaceAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue ||
            actor.Roles.Count != 1 ||
            actor.Roles[0] != RoleNames.Student)
        {
            return StudentPortalQueryResult<StudentPortalWorkspace>
                .Failure(StudentPortalErrorCode.AccessDenied);
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return StudentPortalQueryResult<StudentPortalWorkspace>
                .Failure(StudentPortalErrorCode.SchoolNotActive);
        }

        var snapshot =
            await _portal.GetSnapshotAsync(
                school.Id,
                actorUserId,
                cancellationToken);

        if (snapshot.Profile is null)
        {
            return StudentPortalQueryResult<StudentPortalWorkspace>
                .Failure(StudentPortalErrorCode.ProfileNotLinked);
        }

        var classMap =
            snapshot.ClassGroups.ToDictionary(x => x.Id);

        var yearMap =
            snapshot.AcademicYears.ToDictionary(x => x.Id);

        var gradeMap =
            snapshot.GradeLevels.ToDictionary(x => x.Id);

        var subjectMap =
            snapshot.Subjects.ToDictionary(x => x.Id);

        var versionMap =
            snapshot.FrameworkVersions.ToDictionary(x => x.Id);

        var frameworkMap =
            snapshot.Frameworks.ToDictionary(x => x.Id);

        var enrollmentItems =
            snapshot.Enrollments
                .Select(
                    enrollment =>
                    {
                        if (!classMap.TryGetValue(
                                enrollment.ClassGroupId,
                                out var classGroup) ||
                            !yearMap.TryGetValue(
                                enrollment.AcademicYearId,
                                out var year) ||
                            !gradeMap.TryGetValue(
                                classGroup.GradeLevelId,
                                out var grade))
                        {
                            return null;
                        }

                        return new StudentEnrollmentItem(
                            classGroup.Id,
                            year.Id,
                            grade.Id,
                            classGroup.Name,
                            classGroup.Code,
                            year.Name,
                            grade.Name);
                    })
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray();

        var learning = new List<StudentLearningSubjectItem>();

        foreach (var enrollment in snapshot.Enrollments)
        {
            if (!classMap.TryGetValue(
                    enrollment.ClassGroupId,
                    out var classGroup) ||
                !yearMap.TryGetValue(
                    enrollment.AcademicYearId,
                    out var year) ||
                !gradeMap.TryGetValue(
                    classGroup.GradeLevelId,
                    out var grade))
            {
                continue;
            }

            var adoptionGroups =
                snapshot.CurriculumAdoptions
                    .Where(
                        x =>
                            x.GradeLevelId ==
                                classGroup.GradeLevelId &&
                            (!x.AcademicYearId.HasValue ||
                             x.AcademicYearId.Value ==
                                enrollment.AcademicYearId))
                    .GroupBy(x => x.SubjectId);

            foreach (var group in adoptionGroups)
            {
                var yearSpecific =
                    group
                        .Where(
                            x =>
                                x.AcademicYearId ==
                                enrollment.AcademicYearId)
                        .ToArray();

                var selected =
                    yearSpecific.Length > 0
                        ? yearSpecific
                        : group
                            .Where(x => !x.AcademicYearId.HasValue)
                            .ToArray();

                foreach (var adoption in
                    selected
                        .OrderByDescending(x => x.IsPrimary)
                        .ThenBy(x => x.FrameworkVersionId))
                {
                    if (!subjectMap.TryGetValue(
                            adoption.SubjectId,
                            out var subject) ||
                        !versionMap.TryGetValue(
                            adoption.FrameworkVersionId,
                            out var version) ||
                        !frameworkMap.TryGetValue(
                            version.FrameworkId,
                            out var framework))
                    {
                        continue;
                    }

                    var nodes =
                        snapshot.CurriculumNodes
                            .Where(
                                x =>
                                    x.FrameworkVersionId ==
                                        adoption.FrameworkVersionId &&
                                    x.LogicalLevelFrom <= grade.Order &&
                                    grade.Order <= x.LogicalLevelTo)
                            .OrderBy(x => x.SortOrder)
                            .Select(
                                x =>
                                    new StudentLearningNodeItem(
                                        x.Id,
                                        x.ParentId,
                                        x.NodeKind,
                                        x.Code,
                                        x.Title,
                                        x.Pathway,
                                        x.OfficialText,
                                        x.SortOrder))
                            .ToArray();

                    learning.Add(
                        new StudentLearningSubjectItem(
                            subject.Id,
                            subject.Name,
                            subject.Code,
                            version.Id,
                            framework.Name,
                            version.Name,
                            year.Name,
                            grade.Name,
                            nodes));
                }
            }
        }

        learning =
            learning
                .GroupBy(
                    x =>
                        (
                            x.SubjectId,
                            x.FrameworkVersionId,
                            x.AcademicYearName,
                            x.GradeName))
                .Select(x => x.First())
                .OrderBy(x => x.SubjectName)
                .ThenByDescending(x => x.AcademicYearName)
                .ToList();

        var enrollmentKeys =
            snapshot.Enrollments
                .Select(
                    x =>
                        (
                            x.ClassGroupId,
                            x.AcademicYearId))
                .ToHashSet();

        var openAssessments =
            snapshot.Assessments
                .Where(
                    x =>
                        x.Status == AssessmentStatus.Open &&
                        enrollmentKeys.Contains(
                            (
                                x.ClassGroupId,
                                x.AcademicYearId)))
                .OrderBy(x => x.AssessmentDate)
                .ThenBy(x => x.Title)
                .Select(
                    x =>
                    {
                        classMap.TryGetValue(
                            x.ClassGroupId,
                            out var classGroup);

                        subjectMap.TryGetValue(
                            x.SubjectId,
                            out var subject);

                        return new StudentAssessmentItem(
                            x.Id,
                            x.Title,
                            subject?.Name ?? string.Empty,
                            classGroup?.Name ?? string.Empty,
                            x.AssessmentDate,
                            x.MaxScore);
                    })
                .ToArray();

        var assessmentMap =
            snapshot.Assessments
                .ToDictionary(x => x.Id);

        var resultItems =
            snapshot.Results
                .Where(
                    x =>
                        x.StudentProfileId ==
                        snapshot.Profile.Id)
                .Select(
                    result =>
                    {
                        if (!assessmentMap.TryGetValue(
                                result.AssessmentId,
                                out var assessment))
                        {
                            return null;
                        }

                        subjectMap.TryGetValue(
                            assessment.SubjectId,
                            out var subject);

                        return new StudentResultItem(
                            assessment.Id,
                            assessment.Title,
                            subject?.Name ?? string.Empty,
                            assessment.AssessmentDate,
                            result.Score,
                            assessment.MaxScore,
                            result.Percentage);
                    })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderByDescending(x => x.AssessmentDate)
                .ThenBy(x => x.AssessmentTitle)
                .ToArray();

        return StudentPortalQueryResult<StudentPortalWorkspace>
            .Success(
                new StudentPortalWorkspace(
                    school.Id,
                    school.Name,
                    snapshot.Profile.Id,
                    snapshot.Profile.StudentNumber,
                    snapshot.Profile.DisplayName,
                    enrollmentItems,
                    learning,
                    openAssessments,
                    resultItems));
    }
}
