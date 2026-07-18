using HrmSystem.Domain.Entities.Announcements;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IAnnouncementRepository : IBaseRepository<Announcement, AnnouncementId>
{
    /*
        //?     Offset-paged listing, newest first (DevHabit pagination pattern).
        //?     Returned as (Items, TotalCount) so the handler builds the PaginationResult.
    */
    Task<(IReadOnlyList<Announcement> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    );
}
