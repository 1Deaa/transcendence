using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Application.Common.Interfaces.Data.Queries;

/*
    //?     Dapper read queries over HealthCheckSnapshots + BackupHistory.
    //!     HOST-LEVEL data — the documented exception to the "every Dapper query takes a
    //!     TenantId" rule: these tables have no TenantId column by design (see CLAUDE.md).
*/
public interface IStatusQueries
{
    //? Latest snapshot per component — the status page's component list.
    Task<IReadOnlyList<ComponentHealthResponse>> GetLatestComponentStatesAsync(
        CancellationToken ct
    );

    //? Percentage of non-Unhealthy snapshots over the trailing window.
    Task<double> GetUptimePercentAsync(int days, CancellationToken ct);

    //? Per-day uptime series for the status-page history chart.
    Task<IReadOnlyList<UptimePoint>> GetUptimeHistoryAsync(int days, CancellationToken ct);

    //? Completion time + status of the most recent backup attempt (any outcome).
    Task<LastBackupSummary?> GetLastBackupAsync(CancellationToken ct);
}
