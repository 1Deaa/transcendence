using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Announcements.Shared;

namespace HrmSystem.Application.Features.Announcements.GetAnnouncements;

public sealed record GetAnnouncementsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PaginationResult<AnnouncementResponse>>;
