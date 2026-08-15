using Edulytics.Services.Curriculum;

namespace Edulytics.Web.ViewModels.Curriculum;

public sealed record CurriculumIndexViewModel(
    IReadOnlyList<CurriculumGradeItem> GradeLevels,
    IReadOnlyList<CurriculumSubjectItem> Subjects,
    IReadOnlyList<CurriculumTopicItem> Topics)
{
    public string GradeName(Guid id) =>
        GradeLevels.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;

    public string SubjectName(Guid id) =>
        Subjects.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;
}

public sealed record CurriculumTopicEditViewModel(
    CurriculumTopicItem Topic);

public sealed record LearningOutcomeEditViewModel(
    LearningOutcomeItem Outcome);
