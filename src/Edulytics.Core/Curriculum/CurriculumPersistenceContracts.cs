using Edulytics.Core.Entities;

namespace Edulytics.Core.Curriculum;

public sealed record CurriculumSnapshot(
    IReadOnlyList<GradeLevel> GradeLevels,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<CurriculumTopic> Topics,
    IReadOnlyList<LearningOutcome> Outcomes);

public enum CurriculumPersistenceError
{
    None = 0,
    Constraint = 1
}

public sealed record CurriculumPersistenceResult(
    bool Succeeded,
    CurriculumPersistenceError Error)
{
    public static CurriculumPersistenceResult Success() =>
        new(true, CurriculumPersistenceError.None);

    public static CurriculumPersistenceResult Failure(
        CurriculumPersistenceError error) =>
        new(false, error);
}
