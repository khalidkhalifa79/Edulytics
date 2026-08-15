namespace Edulytics.Services.Realtime;

public sealed record RealtimeGroupResolution(
    bool Succeeded,
    IReadOnlyList<string> Groups)
{
    public static RealtimeGroupResolution Success(
        IReadOnlyList<string> groups) =>
        new(true, groups);

    public static RealtimeGroupResolution Denied() =>
        new(false, []);
}

public interface IRealtimeGroupService
{
    Task<RealtimeGroupResolution> ResolveGroupsAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
