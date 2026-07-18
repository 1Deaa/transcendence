using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Infrastructure.Persistence.Queries;

/*
    //*     Handles ONLY reads for the status/health module using Dapper.
    //!     HOST-LEVEL tables (HealthCheckSnapshots, BackupHistory) — the documented exception
    //!     to the mandatory-TenantId rule: these tables have no TenantId column by design.
*/
public class StatusQueries(ISqlConnectionFactory connectionFactory) : IStatusQueries
{
    public async Task<IReadOnlyList<ComponentHealthResponse>> GetLatestComponentStatesAsync(
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        //? ROW_NUMBER window — one newest snapshot per component, in one round trip.
        const string sql = """
            SELECT ComponentName AS Name, State AS Status, DurationMs, Description, CheckedAtUtc
            FROM (
                SELECT ComponentName, State, DurationMs, Description, CheckedAtUtc,
                       ROW_NUMBER() OVER (PARTITION BY ComponentName ORDER BY CheckedAtUtc DESC) AS RowNo
                FROM [HrmSystem].[HealthCheckSnapshots]
            ) Latest
            WHERE RowNo = 1
            ORDER BY Name
            """;

        var command = new CommandDefinition(commandText: sql, cancellationToken: ct);

        IEnumerable<ComponentHealthResponse> result =
            await connection.QueryAsync<ComponentHealthResponse>(command);

        return result.AsList();
    }

    public async Task<double> GetUptimePercentAsync(int days, CancellationToken ct)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        //? Uptime = share of snapshots that were NOT Unhealthy (Degraded still counts as up).
        const string sql = """
            SELECT CASE WHEN COUNT(*) = 0 THEN 100.0
                        ELSE 100.0 * SUM(CASE WHEN State <> 'Unhealthy' THEN 1 ELSE 0 END) / COUNT(*)
                   END
            FROM [HrmSystem].[HealthCheckSnapshots]
            WHERE CheckedAtUtc >= DATEADD(DAY, -@Days, SYSUTCDATETIME())
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { Days = days },
            cancellationToken: ct
        );

        double uptime = await connection.ExecuteScalarAsync<double>(command);
        return Math.Round(uptime, 3);
    }

    public async Task<IReadOnlyList<UptimePoint>> GetUptimeHistoryAsync(
        int days,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        /*
            //!     UptimePercent is CAST to float on purpose: UptimePoint declares [double],
            //!     and Dapper's positional-record ctor matching rejects a SQL [decimal]
            //!     column against a [double] parameter (500 at materialization time).
        */
        const string sql = """
            SELECT CAST(CheckedAtUtc AS date) AS [Date],
                   CAST(ROUND(100.0 * SUM(CASE WHEN State <> 'Unhealthy' THEN 1 ELSE 0 END) / COUNT(*), 3)
                       AS float) AS UptimePercent
            FROM [HrmSystem].[HealthCheckSnapshots]
            WHERE CheckedAtUtc >= DATEADD(DAY, -@Days, SYSUTCDATETIME())
            GROUP BY CAST(CheckedAtUtc AS date)
            ORDER BY [Date]
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { Days = days },
            cancellationToken: ct
        );

        IEnumerable<UptimePoint> result = await connection.QueryAsync<UptimePoint>(command);
        return result.AsList();
    }

    public async Task<LastBackupSummary?> GetLastBackupAsync(CancellationToken ct)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT TOP 1 CompletedAtUtc, Status
            FROM [HrmSystem].[BackupHistory]
            ORDER BY StartedAtUtc DESC
            """;

        var command = new CommandDefinition(commandText: sql, cancellationToken: ct);

        return await connection.QueryFirstOrDefaultAsync<LastBackupSummary>(command);
    }
}
