using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IBackupHistoryRepository : IBaseRepository<BackupHistory, BackupHistoryId>
{
    //? Newest-first page for the backups dashboard.
    Task<(IReadOnlyList<BackupHistory> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    );

    //? Succeeded backups whose retention window has lapsed — the retention job's work list.
    Task<IReadOnlyList<BackupHistory>> GetExpiredAsync(DateTime utcNow, CancellationToken ct);
}
