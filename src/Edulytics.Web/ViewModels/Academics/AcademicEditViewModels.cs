using Edulytics.Services.Academics;

namespace Edulytics.Web.ViewModels.Academics;

public sealed class AcademicYearEditViewModel
{
    public required AcademicYearItem Year { get; init; }
}

public sealed class ClassGroupEditViewModel
{
    public required ClassGroupItem ClassGroup { get; init; }
    public required IReadOnlyList<GradeLevelItem> GradeLevels { get; init; }
}

public sealed class SubjectEditViewModel
{
    public required SubjectItem Subject { get; init; }
}
