using HrmSystem.Domain.Entities.Announcements;

namespace HrmSystem.Application.Features.Announcements.Shared;

public sealed record AnnouncementResponse(
    string Id,
    string Title,
    string Body,
    string AuthorName,
    DateTime PublishedAtUtc
)
{
    public static AnnouncementResponse FromAnnouncement(Announcement announcement) =>
        new(
            announcement.Id!.Value,
            announcement.Title,
            announcement.Body,
            announcement.AuthorName,
            announcement.PublishedAtUtc
        );
}
