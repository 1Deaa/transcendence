using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Backups.Shared;

namespace HrmSystem.Application.Features.Backups.GetBackupHistory;

public sealed record GetBackupHistoryQuery(int Page = 1, int PageSize = 20)
    : IQuery<PaginationResult<BackupHistoryResponse>>;
