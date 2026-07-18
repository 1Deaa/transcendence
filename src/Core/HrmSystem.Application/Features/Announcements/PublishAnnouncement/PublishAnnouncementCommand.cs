using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Announcements.Shared;

namespace HrmSystem.Application.Features.Announcements.PublishAnnouncement;

public sealed record PublishAnnouncementCommand(string Title, string Body)
    : ICommand<AnnouncementResponse>;
