using Edulytics.Core.Entities;

namespace Edulytics.Core.StudentPortal;

public sealed record StudentPortalSnapshot(
    StudentProfile? Profile,
    IReadOnlyList<StudentEnrollment> Enrollments,
    IReadOnlyList<AcademicYear> AcademicYears,
    IReadOnlyList<ClassGroup> ClassGroups,
    IReadOnlyList<GradeLevel> GradeLevels,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<SchoolCurriculumAdoption> CurriculumAdoptions,
    IReadOnlyList<CurriculumFramework> Frameworks,
    IReadOnlyList<CurriculumFrameworkVersion> FrameworkVersions,
    IReadOnlyList<CurriculumPackContentNode> CurriculumNodes,
    IReadOnlyList<Assessment> Assessments,
    IReadOnlyList<AssessmentResult> Results);
