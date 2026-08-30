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
    /// Exact accepted mappings distinguish aligned lessons from Supporting
    /// lessons. Both are valid canonical-content targets; Supporting lessons
    /// deliberately have no independent official OutcomeCodes.
    /// </summary>
    public static bool IsStandaloneCanonicalTarget(
        int officialOutcomeCount) =>
        officialOutcomeCount > 0;

    public static bool IsSupporting(int officialOutcomeCount) =>
        officialOutcomeCount == 0;

    public static bool IsCanonicalTarget(int officialOutcomeCount) =>
        officialOutcomeCount >= 0;

    public static bool IsProductionReady(
        CanonicalLessonContentStatus? status,
        bool hasOfficialAlignment) =>
        status == CanonicalLessonContentStatus.Published;
}
