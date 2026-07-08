using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.Enums;
using HrmSystem.Domain.Entities.Operations.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class BackupHistoryRepository
    : ABaseRepository<BackupHistory, BackupHistoryId>, IBackupHistoryRepository
{
    public BackupHistoryRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<(IReadOnlyList<BackupHistory> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        IQueryable<BackupHistory> query = DbContext.Set<BackupHistory>().AsNoTracking();

        int totalCount = await query.CountAsync(ct);

        List<BackupHistory> items = await query
            .OrderByDescending(b => b.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<BackupHistory>> GetExpiredAsync(
        DateTime utcNow,
        CancellationToken ct
    )
    {
        //! Tracked on purpose — the retention job mutates these via Purge() then saves.
        return await DbContext
            .Set<BackupHistory>()
            .Where(b =>
                b.Status == BackupStatus.Succeeded
                && b.RetainedUntilUtc != null
                && b.RetainedUntilUtc < utcNow
            )
            .ToListAsync(ct);
    }
}
