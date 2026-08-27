using Edulytics.Core.Enums;
using Edulytics.Core.Lessons;
using Edulytics.Services.LessonContent;

namespace Edulytics.Web.ViewModels.LessonContent;

public sealed record LessonContentIndexViewModel(
    LessonContentDashboard Dashboard,
    bool CanAuthor);

public sealed class LessonContentEditorViewModel
{
    public Guid TopicId { get; set; }
    public Guid? LessonId { get; set; }
    public int Order { get; set; }
    public LearningLessonStatus Status { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public string FrameworkVersionName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public IReadOnlyList<LessonOutcomeRecord> AvailableOutcomes { get; set; } = [];
    public List<Guid> OutcomeIds { get; set; } = [];

    public string EnglishTitle { get; set; } = string.Empty;
    public string EnglishExplanation { get; set; } = string.Empty;
    public string EnglishKeyConceptsAndRules { get; set; } = string.Empty;
    public string EnglishWorkedExamples { get; set; } = string.Empty;
    public string EnglishStepByStepSolutions { get; set; } = string.Empty;
    public string EnglishCommonMistakes { get; set; } = string.Empty;
    public string EnglishQuickSummary { get; set; } = string.Empty;

    public string PolishTitle { get; set; } = string.Empty;
    public string PolishExplanation { get; set; } = string.Empty;
    public string PolishKeyConceptsAndRules { get; set; } = string.Empty;
    public string PolishWorkedExamples { get; set; } = string.Empty;
    public string PolishStepByStepSolutions { get; set; } = string.Empty;
    public string PolishCommonMistakes { get; set; } = string.Empty;
    public string PolishQuickSummary { get; set; } = string.Empty;

    public bool IsNew { get; set; }
    public bool CanAuthor { get; set; }

    public CreateLessonContentRequest ToCreateRequest() =>
        new(
            TopicId,
            Order,
            OutcomeIds,
            EnglishInput(),
            PolishInput());

    public UpdateLessonContentRequest ToUpdateRequest() =>
        new(
            LessonId ?? Guid.Empty,
            Order,
            OutcomeIds,
            EnglishInput(),
            PolishInput());

    private LessonTranslationInput EnglishInput() =>
        new(
            EnglishTitle,
            EnglishExplanation,
            EnglishKeyConceptsAndRules,
            EnglishWorkedExamples,
            EnglishStepByStepSolutions,
            EnglishCommonMistakes,
            EnglishQuickSummary);

    private LessonTranslationInput? PolishInput()
    {
        var input = new LessonTranslationInput(
            PolishTitle,
            PolishExplanation,
            PolishKeyConceptsAndRules,
            PolishWorkedExamples,
            PolishStepByStepSolutions,
            PolishCommonMistakes,
            PolishQuickSummary);

        return LessonContentPolicy.HasAnyContent(input) ? input : null;
    }

    public static LessonContentEditorViewModel From(
        LessonContentEditor editor,
        bool canAuthor) =>
        new()
        {
            TopicId = editor.Topic.TopicId,
            LessonId = editor.LessonId,
            Order = editor.Order,
            Status = editor.Status,
            FrameworkName = editor.Topic.FrameworkName,
            FrameworkVersionName = editor.Topic.FrameworkVersionName,
            SubjectName = editor.Topic.SubjectName,
            SubjectCode = editor.Topic.SubjectCode,
            GradeName = editor.Topic.GradeName,
            TopicName = editor.Topic.TopicName,
            AvailableOutcomes = editor.Topic.Outcomes,
            OutcomeIds = editor.SelectedOutcomeIds.ToList(),
            EnglishTitle = editor.English.Title,
            EnglishExplanation = editor.English.Explanation,
            EnglishKeyConceptsAndRules = editor.English.KeyConceptsAndRules,
            EnglishWorkedExamples = editor.English.WorkedExamples,
            EnglishStepByStepSolutions = editor.English.StepByStepSolutions,
            EnglishCommonMistakes = editor.English.CommonMistakes,
            EnglishQuickSummary = editor.English.QuickSummary,
            PolishTitle = editor.Polish?.Title ?? string.Empty,
            PolishExplanation = editor.Polish?.Explanation ?? string.Empty,
            PolishKeyConceptsAndRules = editor.Polish?.KeyConceptsAndRules ?? string.Empty,
            PolishWorkedExamples = editor.Polish?.WorkedExamples ?? string.Empty,
            PolishStepByStepSolutions = editor.Polish?.StepByStepSolutions ?? string.Empty,
            PolishCommonMistakes = editor.Polish?.CommonMistakes ?? string.Empty,
            PolishQuickSummary = editor.Polish?.QuickSummary ?? string.Empty,
            IsNew = editor.IsNew,
            CanAuthor = canAuthor
        };
}
