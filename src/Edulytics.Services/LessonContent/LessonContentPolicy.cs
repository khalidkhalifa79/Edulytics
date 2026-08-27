using Edulytics.Core.Constants;
using Edulytics.Core.Enums;

namespace Edulytics.Services.LessonContent;

public static class LessonContentPolicy
{
    public static bool CanReadStaff(IReadOnlyList<string> roles) =>
        roles.Count == 1 &&
        (roles[0] == RoleNames.SchoolAdmin ||
         roles[0] == RoleNames.SubjectSupervisor ||
         roles[0] == RoleNames.Teacher);

    public static bool CanAuthor(IReadOnlyList<string> roles) =>
        roles.Count == 1 && roles[0] == RoleNames.SubjectSupervisor;

    public static bool IsStudent(IReadOnlyList<string> roles) =>
        roles.Count == 1 && roles[0] == RoleNames.Student;

    public static bool CanTransition(
        LearningLessonStatus from,
        LearningLessonStatus to) =>
        (from, to) switch
        {
            (LearningLessonStatus.Draft, LearningLessonStatus.InReview) => true,
            (LearningLessonStatus.InReview, LearningLessonStatus.Draft) => true,
            (LearningLessonStatus.InReview, LearningLessonStatus.Published) => true,
            _ => false
        };

    public static bool HasAnyContent(LessonTranslationInput? input)
    {
        if (input is null)
            return false;

        return new[]
        {
            input.Title,
            input.Explanation,
            input.KeyConceptsAndRules,
            input.WorkedExamples,
            input.StepByStepSolutions,
            input.CommonMistakes,
            input.QuickSummary
        }.Any(x => !string.IsNullOrWhiteSpace(x));
    }

    public static bool IsComplete(LessonTranslationInput input) =>
        !string.IsNullOrWhiteSpace(input.Title) &&
        !string.IsNullOrWhiteSpace(input.Explanation) &&
        !string.IsNullOrWhiteSpace(input.KeyConceptsAndRules) &&
        !string.IsNullOrWhiteSpace(input.WorkedExamples) &&
        !string.IsNullOrWhiteSpace(input.StepByStepSolutions) &&
        !string.IsNullOrWhiteSpace(input.CommonMistakes) &&
        !string.IsNullOrWhiteSpace(input.QuickSummary);
}
