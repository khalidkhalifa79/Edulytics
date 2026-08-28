using Edulytics.Core.Constants;
using Edulytics.Core.Enums;
namespace Edulytics.Services.LessonContent;
public static class LessonContentPolicy
{
    public static bool CanReadStaff(IReadOnlyList<string> roles)=>
        roles.Count==1&&(roles[0]==RoleNames.SchoolAdmin||roles[0]==RoleNames.SubjectSupervisor||roles[0]==RoleNames.Teacher);
    public static bool IsStudent(IReadOnlyList<string> roles)=>roles.Count==1&&roles[0]==RoleNames.Student;
    public static bool CanExposeCanonicalBody(CanonicalLessonContentStatus status)=>status==CanonicalLessonContentStatus.Published;
}
