using Edulytics.Services.Notifications;
using Edulytics.Services.LessonContent;
using Edulytics.Services.StudentPortal;

namespace Edulytics.Web.ViewModels.StudentPortal;

public sealed record StudentDashboardViewModel(
    StudentPortalWorkspace Workspace,
    IReadOnlyList<NotificationInboxItem> Notifications)
{
    public int UnreadNotifications =>
        Notifications.Count(x => !x.ReadAtUtc.HasValue);

    public IReadOnlyList<StudentResultItem> RecentResults =>
        Workspace.Results.Take(4).ToArray();
}

public sealed record StudentLearningViewModel(
    StudentPortalWorkspace Workspace,
    IReadOnlyList<StudentLessonSummary> Lessons);

public sealed record StudentAssessmentsViewModel(
    StudentPortalWorkspace Workspace);

public sealed record StudentResultsViewModel(
    StudentPortalWorkspace Workspace);

public sealed record StudentNotificationsViewModel(
    StudentPortalWorkspace Workspace,
    IReadOnlyList<NotificationInboxItem> Notifications);
