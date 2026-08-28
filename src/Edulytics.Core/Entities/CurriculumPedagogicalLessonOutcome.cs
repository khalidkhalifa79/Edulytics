namespace Edulytics.Core.Entities;

/// <summary>
/// Maps an Edulytics pedagogical lesson to an official Standard/Outcome node.
/// </summary>
public sealed class CurriculumPedagogicalLessonOutcome
{
    public Guid PedagogicalLessonId { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid OutcomeNodeId { get; set; }
    public int SortOrder { get; set; }
}
