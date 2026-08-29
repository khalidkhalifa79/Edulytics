using Edulytics.Core.Entities;

namespace Edulytics.Core.Academics;

public sealed record AcademicStructureSnapshot(
    IReadOnlyList<AcademicYear> AcademicYears,
    IReadOnlyList<Term> Terms,
    IReadOnlyList<GradeLevel> GradeLevels,
    IReadOnlyList<ClassGroup> ClassGroups,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<StudentProfile> StudentProfiles,
    IReadOnlyList<TeacherAssignment> TeacherAssignments,
    IReadOnlyList<StudentEnrollment> StudentEnrollments)
{
    public IReadOnlyList<AcademicProgram> AcademicPrograms { get; init; } = [];

    public IReadOnlyList<AcademicYearProgramOffering>
        AcademicYearProgramOfferings
    { get; init; } = [];
}

public enum AcademicPersistenceError
{
    None = 0,
    Conflict = 1,
    Constraint = 2,
    SeatLimit = 3
}

public sealed record AcademicPersistenceResult(
    bool Succeeded,
    AcademicPersistenceError Error)
{
    public static AcademicPersistenceResult Success() =>
        new(true, AcademicPersistenceError.None);

    public static AcademicPersistenceResult Failure(
        AcademicPersistenceError error) =>
        new(false, error);
}
