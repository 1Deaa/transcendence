using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

//? Web-side implementation of the Application notifier port — SignalR stays in this layer.
internal sealed class AnnouncementNotifier(IHubContext<NotificationsHub> hubContext)
    : IAnnouncementNotifier
{
    public async Task PublishAnnouncementAsync(
        TenantId tenantId,
        AnnouncementResponse announcement,
        CancellationToken ct
    )
    {
        await hubContext
            .Clients.Group(NotificationsHub.TenantGroupName(tenantId.Value))
            .SendAsync("AnnouncementPublished", announcement, ct);
    }
}
