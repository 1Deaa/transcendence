using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Backups.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;

namespace HrmSystem.Application.Features.Backups.GetBackupHistory;

internal sealed class GetBackupHistoryQueryHandler(IBackupHistoryRepository backupRepository)
    : IQueryHandler<GetBackupHistoryQuery, PaginationResult<BackupHistoryResponse>>
{
    private readonly IBackupHistoryRepository _backupRepository = backupRepository;

    public async Task<Result<PaginationResult<BackupHistoryResponse>>> Handle(
        GetBackupHistoryQuery query,
        CancellationToken cancellationToken
    )
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<BackupHistory> items, int totalCount) =
            await _backupRepository.GetPagedAsync(page, pageSize, cancellationToken);

        var result =
            PaginationResult<BackupHistoryResponse>.Create(
                items.Select(BackupHistoryResponse.FromBackupHistory).ToList(),
                page,
                pageSize,
                totalCount
            );

        return result;
    }
}
