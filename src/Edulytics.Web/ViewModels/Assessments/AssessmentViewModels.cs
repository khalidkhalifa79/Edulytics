using Edulytics.Core.Enums;
using Edulytics.Services.Assessments;

namespace Edulytics.Web.ViewModels.Assessments;

public sealed record AssessmentIndexViewModel(AssessmentWorkspace Workspace)
{
    public string ClassName(Guid id) =>
        Workspace.ClassGroups.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;

    public string SubjectName(Guid id) =>
        Workspace.Subjects.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;

    public string TermName(Guid id) =>
        Workspace.Terms.FirstOrDefault(x => x.Id == id)?.Name ?? string.Empty;
}

public sealed record AssessmentDetailsViewModel(AssessmentDetails Details)
{
    public string ClassName =>
        Details.ClassGroups.FirstOrDefault(x => x.Id == Details.Assessment.ClassGroupId)?.Name ?? string.Empty;

    public string SubjectName =>
        Details.Subjects.FirstOrDefault(x => x.Id == Details.Assessment.SubjectId)?.Name ?? string.Empty;

    public string TermName =>
        Details.Terms.FirstOrDefault(x => x.Id == Details.Assessment.TermId)?.Name ?? string.Empty;

    public AssessmentOutcomeItem? Outcome(Guid id) =>
        Details.EligibleOutcomes.FirstOrDefault(x => x.Id == id);

    public string StatusKey =>
        Details.Assessment.Status switch
        {
            AssessmentStatus.Draft => "StatusDraft",
            AssessmentStatus.Open => "StatusOpen",
            _ => "StatusClosed"
        };
}

public sealed record AssessmentEditViewModel(AssessmentDetails Details);

public sealed record AssessmentQuestionEditViewModel(
    Guid AssessmentId,
    AssessmentQuestionItem Question,
    byte[] AssessmentRowVersion);

public sealed record AssessmentResultsViewModel(AssessmentResultsWorkspace Workspace)
{
    public string StatusKey =>
        Workspace.Assessment.Status switch
        {
            AssessmentStatus.Draft => "StatusDraft",
            AssessmentStatus.Open => "StatusOpen",
            _ => "StatusClosed"
        };
}
