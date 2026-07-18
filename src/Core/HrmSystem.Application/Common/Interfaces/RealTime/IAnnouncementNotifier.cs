using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.RealTime;

/*
    //?     Application port for the real-time notifications channel (NotificationsHub in Web):
    //?     every connected employee of the tenant receives the announcement instantly.
*/
public interface IAnnouncementNotifier
{
    Task PublishAnnouncementAsync(
        TenantId tenantId,
        AnnouncementResponse announcement,
        CancellationToken ct
    );
}
