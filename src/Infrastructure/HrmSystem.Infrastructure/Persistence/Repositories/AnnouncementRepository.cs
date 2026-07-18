using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Announcements;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class AnnouncementRepository
    : ABaseRepository<Announcement, AnnouncementId>, IAnnouncementRepository
{
    public AnnouncementRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<(IReadOnlyList<Announcement> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        //? The named tenant query filter scopes both queries to the caller's workspace.
        IQueryable<Announcement> query = DbContext
            .Set<Announcement>()
            .AsNoTracking()
            .OrderByDescending(a => a.PublishedAtUtc);

        int totalCount = await query.CountAsync(ct);

        List<Announcement> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
