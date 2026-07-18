using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Announcements;

namespace HrmSystem.Application.Features.Announcements.GetAnnouncements;

internal sealed class GetAnnouncementsQueryHandler(
    IAnnouncementRepository announcementRepository
) : IQueryHandler<GetAnnouncementsQuery, PaginationResult<AnnouncementResponse>>
{
    private readonly IAnnouncementRepository _announcementRepository = announcementRepository;

    public async Task<Result<PaginationResult<AnnouncementResponse>>> Handle(
        GetAnnouncementsQuery query,
        CancellationToken cancellationToken
    )
    {
        (IReadOnlyList<Announcement> items, int totalCount) =
            await _announcementRepository.GetPagedAsync(
                query.Page,
                query.PageSize,
                cancellationToken
            );

        return PaginationResult<AnnouncementResponse>.Create(
            items.Select(AnnouncementResponse.FromAnnouncement).ToList(),
            query.Page,
            query.PageSize,
            totalCount
        );
    }
}
