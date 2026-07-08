using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     Nightly housekeeping (03:00 UTC), maintenance-crosses-all-tenants by design:
    //?      1. Deletes finished import jobs older than 7 days (their file payload is already
    //?         cleared, this drops the audit rows).
    //?      2. Marks Pending import jobs older than 1 hour as Failed — a restart lost their
    //?         trigger (RAMJobStore), nothing will ever pick them up.
    //?      3. Purges HealthCheckSnapshots older than 90 days (uptime history window).
    //
    //!     Uses IgnoreQueryFilters + ExecuteUpdate/DeleteAsync — set-based maintenance SQL,
    //!     intentionally bypassing interceptors and per-entity domain methods.
*/
[DisallowConcurrentExecution]
internal sealed class CleanupImportJobsJob(
    ApplicationDbContext db,
    IDateTimeProvider dateTimeProvider,
    ILogger<CleanupImportJobsJob> logger
) : IJob
{
    public static readonly JobKey Key = new("cleanup-imports", "operations");

    public async Task Execute(IJobExecutionContext context)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        DateTime finishedCutoff = utcNow.AddDays(-7);
        DateTime staleCutoff = utcNow.AddHours(-1);
        DateTime snapshotCutoff = utcNow.AddDays(-90);

        int deletedJobs = await db
            .ImportJobs.IgnoreQueryFilters()
            .Where(j =>
                (j.Status == Domain.Entities.Imports.Enums.ImportStatus.Completed
                    || j.Status == Domain.Entities.Imports.Enums.ImportStatus.Failed)
                && j.CompletedAtUtc != null
                && j.CompletedAtUtc < finishedCutoff
            )
            .ExecuteDeleteAsync(context.CancellationToken);

        int staleJobs = await db
            .ImportJobs.IgnoreQueryFilters()
            .Where(j =>
                j.Status == Domain.Entities.Imports.Enums.ImportStatus.Pending
                && j.CreatedAt < staleCutoff
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(j => j.Status, Domain.Entities.Imports.Enums.ImportStatus.Failed)
                        .SetProperty(j => j.CompletedAtUtc, utcNow)
                        .SetProperty(
                            j => j.ErrorLog,
                            "The job was never picked up (scheduler restart) and was marked failed by cleanup."
                        ),
                context.CancellationToken
            );

        int purgedSnapshots = await db
            .HealthCheckSnapshots.Where(s => s.CheckedAtUtc < snapshotCutoff)
            .ExecuteDeleteAsync(context.CancellationToken);

        logger.LogInformation(
            "Cleanup: {DeletedJobs} old import jobs deleted, {StaleJobs} stale Pending jobs failed, {PurgedSnapshots} snapshots purged.",
            deletedJobs,
            staleJobs,
            purgedSnapshots
        );
    }
}
