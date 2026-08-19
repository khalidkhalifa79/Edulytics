namespace Edulytics.Core.Realtime;

public static class RealtimeEventTypes
{
    public const string AssessmentResultEntered =
        "AssessmentResultEntered";

    public const string AssessmentResultUpdated =
        "AssessmentResultUpdated";

    public const string ImportBatchCompleted =
        "ImportBatchCompleted";
}

public static class RealtimeGroupNames
{
    public static string SchoolAnalytics(Guid schoolId) =>
        $"school:{schoolId:N}:analytics";

    public static string SchoolAdmins(Guid schoolId) =>
        $"school:{schoolId:N}:admins";

    public static string Teachers(
        Guid schoolId,
        Guid classGroupId,
        Guid subjectId) =>
        $"school:{schoolId:N}:class:{classGroupId:N}:subject:{subjectId:N}:teachers";

    public static string SubjectSupervisors(
        Guid schoolId,
        Guid subjectId) =>
        $"school:{schoolId:N}:subject:{subjectId:N}:supervisors";
}

public sealed record AssessmentResultChangedEvent(
    Guid EventId,
    Guid SchoolId,
    Guid AssessmentId,
    Guid AssessmentResultId,
    Guid ClassGroupId,
    Guid SubjectId,
    Guid StudentProfileId,
    DateTime OccurredAtUtc);

public sealed record ImportDashboardScope(
    Guid ClassGroupId,
    Guid SubjectId);

public sealed record ImportBatchCompletedEvent(
    Guid EventId,
    Guid SchoolId,
    Guid ImportBatchId,
    string ImportType,
    IReadOnlyList<ImportDashboardScope> AffectedScopes,
    DateTime OccurredAtUtc);

public sealed record AnalyticsInvalidationMessage(
    Guid RefreshId,
    Guid SchoolId,
    DateTime UpdatedAtUtc);

public sealed record DashboardUpdatedMessage(
    Guid EventId,
    Guid AssessmentId,
    Guid ClassGroupId,
    Guid SubjectId,
    DateTime UpdatedAtUtc);
