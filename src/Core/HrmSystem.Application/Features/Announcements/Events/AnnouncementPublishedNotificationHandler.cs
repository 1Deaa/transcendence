using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Entities.Announcements;
using HrmSystem.Domain.Entities.Announcements.Events;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using MediatR;

namespace HrmSystem.Application.Features.Announcements.Events;

/*
    //?     Fans a freshly-published announcement out to every connected employee of the
    //?     tenant via the notifications hub. Runs AFTER SaveChangesAsync commits, so the
    //?     pushed payload always exists in the database.
    //!     The tenant comes from the ambient ITenantContext — the event fires inside the
    //!     same scope that performed the write; no tenant in scope → nothing to push.
*/
internal sealed class AnnouncementPublishedNotificationHandler(
    IAnnouncementRepository announcementRepository,
    IAnnouncementNotifier announcementNotifier,
    ITenantContext tenantContext
) : INotificationHandler<AnnouncementPublishedDomainEvent>
{
    public async Task Handle(
        AnnouncementPublishedDomainEvent notification,
        CancellationToken cancellationToken
    )
    {
        TenantId? tenantId = tenantContext.Current;
        if (tenantId is null)
        {
            return;
        }

        Announcement? announcement = await announcementRepository.GetByIdAsync(
            notification.AnnouncementId,
            cancellationToken
        );
        if (announcement is null)
        {
            return;
        }

        await announcementNotifier.PublishAnnouncementAsync(
            tenantId,
            AnnouncementResponse.FromAnnouncement(announcement),
            cancellationToken
        );
    }
}
