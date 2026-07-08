using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Status.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.Enums;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     The status page's heartbeat (every HealthOptions.ProbeIntervalSeconds):
    //?      1. Runs every registered IHealthCheck via HealthCheckService.
    //?      2. Persists one HealthCheckSnapshot per component (uptime raw material).
    //?      3. On a CONFIRMED overall-state transition, pushes the fresh public status
    //?         through IStatusNotifier → StatusHub → connected status pages.
    //
    //!     Anti-flapping: a new overall state must be observed on TWO consecutive probes
    //!     before it is broadcast — a single failed probe never flips the public page.
*/
[DisallowConcurrentExecution]
internal sealed class HealthProbeJob(
    HealthCheckService healthCheckService,
    ApplicationDbContext db,
    IUnitOfWork unitOfWork,
    IStatusQueries statusQueries,
    IStatusNotifier statusNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<HealthProbeJob> logger
) : IJob
{
    public static readonly JobKey Key = new("health-probe", "operations");

    public async Task Execute(IJobExecutionContext context)
    {
        HealthReport report = await healthCheckService.CheckHealthAsync(
            context.CancellationToken
        );
        DateTime checkedAt = dateTimeProvider.UtcNow;

        foreach ((string name, HealthReportEntry entry) in report.Entries)
        {
            Result<HealthCheckSnapshot> snapshotResult = HealthCheckSnapshot.Record(
                name,
                MapState(entry.Status),
                entry.Duration.TotalMilliseconds,
                entry.Description ?? entry.Exception?.Message,
                checkedAt
            );

            if (snapshotResult.IsSuccess)
            {
                await db.HealthCheckSnapshots.AddAsync(
                    snapshotResult.Value,
                    context.CancellationToken
                );
            }
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        string overallState = MapState(report.Status).ToString();
        if (TransitionTracker.ShouldBroadcast(overallState))
        {
            await BroadcastAsync(overallState, context.CancellationToken);
        }
    }

    private async Task BroadcastAsync(string overallState, CancellationToken ct)
    {
        //? Reuse the shared builder so push and pull agree on shape + redaction rules.
        IReadOnlyList<ComponentHealthResponse> components =
            await statusQueries.GetLatestComponentStatesAsync(ct);
        double uptime30 = await statusQueries.GetUptimePercentAsync(30, ct);
        double uptime90 = await statusQueries.GetUptimePercentAsync(90, ct);
        LastBackupSummary? lastBackup = await statusQueries.GetLastBackupAsync(ct);

        PublicStatusResponse status = PublicStatusBuilder.Build(
            components,
            uptime30,
            uptime90,
            lastBackup
        );

        await statusNotifier.PublishStatusChangedAsync(status, ct);

        logger.LogInformation("Status transition broadcast: {OverallState}.", overallState);
    }

    private static HealthState MapState(HealthStatus status) =>
        status switch
        {
            HealthStatus.Healthy => HealthState.Healthy,
            HealthStatus.Degraded => HealthState.Degraded,
            _ => HealthState.Unhealthy,
        };

    /*
        //?     Transition memory spanning job ticks — Quartz creates a fresh scoped job
        //?     instance per execution, so this state must live outside the instance.
        //!     Static-only members keep Sonar S2696 happy; single-writer is guaranteed by
        //!     [DisallowConcurrentExecution] on the enclosing job.
    */
    private static class TransitionTracker
    {
        private static string? s_lastBroadcastState;
        private static string? s_lastObservedState;
        private static int s_observedStreak;

        public static bool ShouldBroadcast(string overallState)
        {
            if (overallState == s_lastObservedState)
            {
                s_observedStreak++;
            }
            else
            {
                s_lastObservedState = overallState;
                s_observedStreak = 1;
            }

            bool isConfirmed = s_observedStreak >= 2 || s_lastBroadcastState is null;
            if (!isConfirmed || overallState == s_lastBroadcastState)
            {
                return false;
            }

            s_lastBroadcastState = overallState;
            return true;
        }
    }
}
