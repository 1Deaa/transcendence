using HrmSystem.Application.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

/*
    //?     Live CSV-import progress channel — JWT-authenticated, per-tenant groups
    //?     (same pattern as AnalyticsHub). Push is the enhancement; polling is the truth.
*/
[Authorize]
public sealed class ImportProgressHub : Hub
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

    public static string TenantGroupName(string tenantId) => $"tenant:{tenantId}:imports";
}
