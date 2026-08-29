using Edulytics.Core.Constants;
using Edulytics.Core.Enums;

namespace Edulytics.Services.LessonContent;

public static class LessonContentPolicy
{
    public static bool CanReadStaff(
        IReadOnlyList<string> roles) =>
        roles.Count == 1 &&
        (
            roles[0] == RoleNames.SchoolAdmin ||
            roles[0] == RoleNames.SubjectSupervisor ||
            roles[0] == RoleNames.Teacher
        );

    public static bool IsStudent(
        IReadOnlyList<string> roles) =>
        roles.Count == 1 &&
        roles[0] == RoleNames.Student;

    public static bool CanExposeCanonicalBody(
        CanonicalLessonContentStatus status) =>
        status ==
        CanonicalLessonContentStatus.Published;

    /// <summary>
    /// A pedagogical lesson is a standalone canonical-content target only
    /// when it has at least one exact accepted official outcome/standard.
    ///
    /// A zero-formal lesson may still be a valid source-derived supporting
    /// lesson, but that classification must first be proven against the
    /// accepted source blueprint. We never invent an official mapping merely
    /// to make a supporting lesson independently publishable.
    /// </summary>
    public static bool IsStandaloneCanonicalTarget(
        int officialOutcomeCount) =>
        officialOutcomeCount > 0;

    public static bool IsProductionReady(
        CanonicalLessonContentStatus? status,
        bool hasOfficialAlignment) =>
        status ==
            CanonicalLessonContentStatus.Published &&
        hasOfficialAlignment;
}
