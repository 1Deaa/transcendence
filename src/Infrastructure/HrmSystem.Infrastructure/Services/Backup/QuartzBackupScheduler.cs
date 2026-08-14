using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Operations;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HrmSystem.Infrastructure.Services.Backup;

/*
    //?     IBackupScheduler over Quartz — enqueues an immediate DatabaseBackupJob run.
    //!     Refuses with BackupErrors.SchedulerUnavailable (→ 503) when the scheduler is
    //!     stopped or in standby — mirrors QuartzSchedulerHealthCheck exactly.
*/
internal sealed class QuartzBackupScheduler(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzBackupScheduler> logger
) : IBackupScheduler
{
    public async Task<Result> TriggerBackupAsync(CancellationToken ct)
    {
        try
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(ct);

            if (!scheduler.IsStarted || scheduler.InStandbyMode)
            {
                return BackupErrors.SchedulerUnavailable;
            }

            await scheduler.TriggerJob(DatabaseBackupJob.Key, ct);

            return Result.Success();
        }
        catch (SchedulerException exception)
        {
            logger.LogError(exception, "Backup trigger failed at the scheduler level.");
            return BackupErrors.TriggerFailed;
        }
    }
}
