using Edulytics.Services.Curriculum;

namespace Edulytics.Web.ViewModels.Curriculum;

public sealed record CurriculumIndexViewModel(
    IReadOnlyList<CurriculumGradeItem> GradeLevels,
    IReadOnlyList<CurriculumSubjectItem> Subjects,
    IReadOnlyList<CurriculumTopicItem> Topics)
{
    public IReadOnlyList<CurriculumProgramItem> AcademicPrograms { get; init; } = [];

    public IReadOnlyList<CurriculumFrameworkItem> Frameworks
    {
        get;
        init;
    } = [];

    public IReadOnlyList<CurriculumAdoptionItem> Adoptions
    {
        get;
        init;
    } = [];

    public string GradeName(Guid id) =>
        GradeLevels.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;

    public string SubjectName(Guid id) =>
        Subjects.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;

    public string OfficialSelectionValue(
        OfficialCurriculumOutcomeOption outcome) =>
        outcome.LessonNodeId.HasValue
            ? $"{outcome.ContentNodeId:D}|{outcome.LessonNodeId.Value:D}"
            : outcome.ContentNodeId.ToString("D");
}

public sealed record CurriculumTopicEditViewModel(
    CurriculumTopicItem Topic);

public sealed record LearningOutcomeEditViewModel(
    LearningOutcomeItem Outcome);
