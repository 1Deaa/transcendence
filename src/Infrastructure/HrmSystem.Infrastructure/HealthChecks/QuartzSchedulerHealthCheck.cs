using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace HrmSystem.Infrastructure.HealthChecks;

/*
    //?     Verifies the Quartz scheduler is started and not in standby — backups, health
    //?     probes and CSV imports all silently stop when the scheduler is down.
    //!     TriggerBackupCommand refuses with 503 (BackupErrors.SchedulerUnavailable)
    //!     while this check is Unhealthy.
*/
internal sealed class QuartzSchedulerHealthCheck(ISchedulerFactory schedulerFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            if (scheduler.IsStarted && !scheduler.InStandbyMode)
            {
                return HealthCheckResult.Healthy("Scheduler is running.");
            }

            return HealthCheckResult.Unhealthy(
                $"Scheduler state — started: {scheduler.IsStarted}, standby: {scheduler.InStandbyMode}."
            );
        }
        catch (SchedulerException exception)
        {
            return HealthCheckResult.Unhealthy(
                "Scheduler could not be resolved.",
                exception
            );
        }
    }
}
