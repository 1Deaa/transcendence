using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Infrastructure.Services.Backup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     Daily retention sweep (default 03:30 UTC): deletes expired .bak files and marks
    //?     their history rows Purged. The retention window was stamped by DatabaseBackupJob.
    //
    //!     File deletion only works where the backups volume is mounted (the containerized
    //!     API mounts sqlserver_backups at /var/backups). When the file is unreachable
    //!     (e.g. API running on the host during dev) the row is left untouched so a later
    //!     containerized run can actually delete the file — never mark Purged blind.
*/
[DisallowConcurrentExecution]
internal sealed class BackupRetentionJob(
    IBackupHistoryRepository backupRepository,
    IUnitOfWork unitOfWork,
    IOptions<BackupOptions> options,
    IDateTimeProvider dateTimeProvider,
    ILogger<BackupRetentionJob> logger
) : IJob
{
    public static readonly JobKey Key = new("backup-retention", "operations");

    private readonly BackupOptions _options = options.Value;

    public async Task Execute(IJobExecutionContext context)
    {
        IReadOnlyList<BackupHistory> expired = await backupRepository.GetExpiredAsync(
            dateTimeProvider.UtcNow,
            context.CancellationToken
        );

        if (expired.Count == 0)
        {
            return;
        }

        int purged = 0;

        foreach (BackupHistory backup in expired)
        {
            string fullPath = $"{_options.Directory.TrimEnd('/')}/{backup.FileName}";

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else if (!Directory.Exists(_options.Directory))
                {
                    //! Volume not mounted here — leave the row for a run that can reach the file.
                    logger.LogWarning(
                        "Backups directory '{Directory}' is not reachable — skipping purge of '{FileName}'.",
                        _options.Directory,
                        backup.FileName
                    );
                    continue;
                }

                //? File gone (deleted now, or already missing while the volume IS mounted) → purge.
                Result purgeResult = backup.Purge();
                if (purgeResult.IsSuccess)
                {
                    purged++;
                }
            }
            catch (IOException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not delete expired backup '{FileName}' — retrying next run.",
                    backup.FileName
                );
            }
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Backup retention sweep purged {Purged}/{Expired} expired backups.",
            purged,
            expired.Count
        );
    }
}
