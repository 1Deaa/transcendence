using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;

namespace HrmSystem.Domain.Entities.Announcements.Events;

//? Raised on publish; the Application handler pushes it to the tenant's notification hub group.
public sealed record AnnouncementPublishedDomainEvent(AnnouncementId AnnouncementId)
    : IDomainEvent;
