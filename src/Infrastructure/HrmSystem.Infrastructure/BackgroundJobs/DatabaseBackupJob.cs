using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.Enums;
using HrmSystem.Infrastructure.Services.Backup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     Nightly full database backup (cron from BackupOptions, default 02:00 UTC) —
    //?     also triggered on demand via POST /api/backups/run (TriggerBackupCommand).
    //?     Flow: Start() row → BACKUP DATABASE … WITH COMPRESSION, CHECKSUM → Complete()/Fail().
    //
    //?     Retention stamped at completion: first success of the ISO week keeps
    //?     WeeklyRetentionDays, every other one DailyRetentionDays (the "7 daily + 4 weekly" policy).
    //
    //!     [DisallowConcurrentExecution] — two overlapping BACKUP DATABASE runs thrash I/O
    //!     and produce ambiguous "latest" ordering in msdb.
*/
[DisallowConcurrentExecution]
internal sealed class DatabaseBackupJob(
    IBackupHistoryRepository backupRepository,
    IUnitOfWork unitOfWork,
    SqlServerBackupService backupService,
    IDateTimeProvider dateTimeProvider,
    IOptions<BackupOptions> options,
    ILogger<DatabaseBackupJob> logger
) : IJob
{
    public static readonly JobKey Key = new("database-backup", "operations");

    private readonly BackupOptions _options = options.Value;

    public async Task Execute(IJobExecutionContext context)
    {
        DateTime startedAt = dateTimeProvider.UtcNow;
        string fileName = backupService.BuildBackupFileName(startedAt);

        Result<BackupHistory> historyResult = BackupHistory.Start(fileName, startedAt);
        if (historyResult.IsFailure)
        {
            logger.LogError("BackupHistory.Start failed — backup aborted.");
            return;
        }

        BackupHistory history = historyResult.Value;
        await backupRepository.AddAsync(history, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        try
        {
            long sizeBytes = await backupService.ExecuteBackupAsync(
                fileName,
                context.CancellationToken
            );

            DateTime completedAt = dateTimeProvider.UtcNow;
            DateTime retainedUntil = completedAt.AddDays(
                await IsFirstSuccessOfIsoWeekAsync(completedAt, context.CancellationToken)
                    ? _options.WeeklyRetentionDays
                    : _options.DailyRetentionDays
            );

            Result completeResult = history.Complete(completedAt, sizeBytes, retainedUntil);
            if (completeResult.IsFailure)
            {
                logger.LogError("BackupHistory.Complete was rejected — state mismatch.");
            }

            logger.LogInformation(
                "Database backup '{FileName}' completed ({SizeMb:F1} MB, retained until {RetainedUntil:yyyy-MM-dd}).",
                fileName,
                sizeBytes / 1_048_576d,
                retainedUntil
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            //! Capture the failure on the history row — the freshness check + dashboard rely on it.
            Result failResult = history.Fail(dateTimeProvider.UtcNow, exception.Message);
            if (failResult.IsFailure)
            {
                logger.LogError("BackupHistory.Fail was rejected — state mismatch.");
            }

            logger.LogError(exception, "Database backup '{FileName}' failed.", fileName);
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }

    private async Task<bool> IsFirstSuccessOfIsoWeekAsync(DateTime utcNow, CancellationToken ct)
    {
        //? Monday 00:00 UTC of the current ISO week.
        int daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7;
        DateTime weekStart = utcNow.Date.AddDays(-daysSinceMonday);

        IEnumerable<BackupHistory> thisWeek = await backupRepository.FindUsingPredicate(
            b => b.Status == BackupStatus.Succeeded && b.CompletedAtUtc >= weekStart,
            ct
        );

        return !thisWeek.Any();
    }
}
