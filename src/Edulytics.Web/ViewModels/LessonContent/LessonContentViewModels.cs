using Edulytics.Services.LessonContent;
namespace Edulytics.Web.ViewModels.LessonContent;
public sealed record LessonContentIndexViewModel(LessonContentDashboard Dashboard);
public sealed record LessonContentDetailViewModel(CanonicalLessonDetail Lesson);
