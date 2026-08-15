using System.Security.Claims;
using Edulytics.Services.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Edulytics.Web.Hubs;

[Authorize(Policy = "AnalyticsRead")]
public sealed class AnalyticsHub : Hub
{
    private readonly IRealtimeGroupService _groups;

    public AnalyticsHub(IRealtimeGroupService groups)
    {
        _groups = groups;
    }

    public override async Task OnConnectedAsync()
    {
        var value =
            Context.User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var actorUserId))
        {
            Context.Abort();
            return;
        }

        var resolution =
            await _groups.ResolveGroupsAsync(
                actorUserId,
                Context.ConnectionAborted);

        if (!resolution.Succeeded)
        {
            Context.Abort();
            return;
        }

        foreach (var group in resolution.Groups)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                group,
                Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }
}
