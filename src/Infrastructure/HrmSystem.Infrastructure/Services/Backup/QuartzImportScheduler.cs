using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Operations;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports.ValueObjects;
using HrmSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HrmSystem.Infrastructure.Services.Backup;

/*
    //?     IImportScheduler over Quartz — fires ProcessImportJob NOW with the ImportJobId
    //?     in the trigger's JobDataMap (merged into the job's map at execution time).
*/
internal sealed class QuartzImportScheduler(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzImportScheduler> logger
) : IImportScheduler
{
    public async Task<Result> EnqueueImportAsync(ImportJobId importJobId, CancellationToken ct)
    {
        try
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(ct);

            if (!scheduler.IsStarted || scheduler.InStandbyMode)
            {
                return BackupErrors.SchedulerUnavailable;
            }

            var dataMap = new JobDataMap
            {
                { ProcessImportJob.ImportJobIdDataKey, importJobId.Value },
            };

            await scheduler.TriggerJob(ProcessImportJob.Key, dataMap, ct);

            return Result.Success();
        }
        catch (SchedulerException exception)
        {
            logger.LogError(exception, "Import enqueue failed at the scheduler level.");
            return BackupErrors.TriggerFailed;
        }
    }
}
