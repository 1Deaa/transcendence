using HrmSystem.Application.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

/*
    //?     Company-wide notification channel (announcements, future: approvals, mentions).
    //?     JWT-authenticated; each connection joins its tenant's group from the tenant_id
    //?     claim, so a broadcast can never cross workspaces.
    //!     Host-level users (no tenant claim) connect but join no group — they receive nothing.
*/
[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        string? tenantId = Context.User?.FindFirst(CustomClaimTypes.TenantId)?.Value;

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroupName(tenantId));
        }

        /*
            //?     Per-user group — targets ONE person across all their tabs/devices.
            //?     Used by ChatNotifier for direct-message delivery; keyed on the domain
            //?     UserId so the payload ids and the group key speak the same language.
        */
        string? domainUserId = Context.User?.FindFirst(CustomClaimTypes.DomainUserId)?.Value;

        if (!string.IsNullOrEmpty(domainUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(domainUserId));
        }

        await base.OnConnectedAsync();
    }

    //> Shared with AnnouncementNotifier so group names never drift apart.
    public static string TenantGroupName(string tenantId) => $"tenant:{tenantId}";

    //> Shared with ChatNotifier so group names never drift apart.
    public static string UserGroupName(string domainUserId) => $"user:{domainUserId}";
}
