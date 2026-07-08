using HrmSystem.Application.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

/*
    //?     Live-dashboard push channel — JWT-authenticated (access_token query string on the
    //?     WebSocket handshake, see JwtBearerOptionsSetup.OnMessageReceived).
    //?     Each connection joins its tenant's group from the tenant_id claim, so a
    //?     "MetricsChanged" broadcast can never cross workspaces.
    //!     Host-level users (no tenant claim) connect but join no group — they receive nothing.
*/
[Authorize]
public sealed class AnalyticsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        string? tenantId = Context.User?.FindFirst(CustomClaimTypes.TenantId)?.Value;

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroupName(tenantId));
        }

        await base.OnConnectedAsync();
    }

    //> Shared with AnalyticsNotifier so group names never drift apart.
    public static string TenantGroupName(string tenantId) => $"tenant:{tenantId}";
}
