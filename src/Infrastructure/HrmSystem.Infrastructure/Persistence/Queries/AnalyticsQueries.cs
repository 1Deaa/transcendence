using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Infrastructure.Persistence.Queries;

/*
    //*     Handles ONLY analytics reads using Dapper — optimized aggregate SQL.
    //!     ABSOLUTE RULE: every statement filters WHERE TenantId = @TenantId — Dapper
    //!     bypasses the EF query filters, so this clause IS the tenant isolation.
    //!     Soft delete respected the same way: AND IsDeleted = 0 on every tenant table.
*/
public class AnalyticsQueries(ISqlConnectionFactory connectionFactory) : IAnalyticsQueries
{
    public async Task<KpiSummary> GetDashboardSummaryAsync(
        TenantId tenantId,
        DateOnly today,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        //? One round trip, five scalar sub-selects — cheaper than five queries.
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM [HrmSystem].[Employees]
                  WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status <> 'Terminated') AS TotalEmployees,
                (SELECT COUNT(*) FROM [HrmSystem].[Attendances]
                  WHERE TenantId = @TenantId AND IsDeleted = 0 AND [Date] = @Today AND Status = 'Present') AS PresentToday,
                (SELECT COUNT(*) FROM [HrmSystem].[Attendances]
                  WHERE TenantId = @TenantId AND IsDeleted = 0 AND [Date] = @Today AND Status = 'Late') AS LateToday,
                (SELECT COUNT(*) FROM [HrmSystem].[Attendances]
                  WHERE TenantId = @TenantId AND IsDeleted = 0 AND [Date] = @Today AND Status = 'Absent') AS AbsentToday,
                (SELECT COUNT(*) FROM [HrmSystem].[LeaveRequests]
                  WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 'Pending') AS PendingLeaves,
                (SELECT COUNT(*) FROM [HrmSystem].[Departments]
                  WHERE TenantId = @TenantId AND IsDeleted = 0) AS Departments
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value, Today = today },
            cancellationToken: ct
        );

        return await connection.QuerySingleAsync<KpiSummary>(command);
    }

    public async Task<IReadOnlyList<ChartSeries>> GetAttendanceTrendAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        string? departmentId,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT a.[Date],
                   SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) AS Present,
                   SUM(CASE WHEN a.Status = 'Late'    THEN 1 ELSE 0 END) AS Late,
                   SUM(CASE WHEN a.Status = 'Absent'  THEN 1 ELSE 0 END) AS Absent
            FROM [HrmSystem].[Attendances] a
            JOIN [HrmSystem].[Employees] e ON e.Id = a.EmployeeId AND e.TenantId = @TenantId
            WHERE a.TenantId = @TenantId AND a.IsDeleted = 0
              AND a.[Date] BETWEEN @From AND @To
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
            GROUP BY a.[Date]
            ORDER BY a.[Date]
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new
            {
                TenantId = tenantId.Value,
                From = from,
                To = to,
                DepartmentId = departmentId,
            },
            cancellationToken: ct
        );

        IEnumerable<AttendanceTrendRow> rows =
            await connection.QueryAsync<AttendanceTrendRow>(command);
        List<AttendanceTrendRow> rowList = rows.AsList();

        //? Pivot rows into three named series — exactly the ApexCharts series[] shape.
        return
        [
            new ChartSeries(
                "Present",
                rowList.Select(r => new TimeSeriesPoint(r.Date, r.Present)).ToList()
            ),
            new ChartSeries(
                "Late",
                rowList.Select(r => new TimeSeriesPoint(r.Date, r.Late)).ToList()
            ),
            new ChartSeries(
                "Absent",
                rowList.Select(r => new TimeSeriesPoint(r.Date, r.Absent)).ToList()
            ),
        ];
    }

    public async Task<IReadOnlyList<TimeSeriesPoint>> GetHeadcountTrendAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        /*
            //?     Recursive date spine over the window, then for each day count employees
            //?     hired on/before it and not yet terminated — cumulative headcount.
            //!     MAXRECURSION 400 covers the validator-enforced 366-day ceiling.
        */
        const string sql = """
            WITH Dates AS (
                SELECT @From AS [Date]
                UNION ALL
                SELECT DATEADD(DAY, 1, [Date]) FROM Dates WHERE [Date] < @To
            )
            SELECT d.[Date],
                   --! CAST to decimal — Dapper matches the TimeSeriesPoint ctor by EXACT type.
                   CAST((SELECT COUNT(*) FROM [HrmSystem].[Employees] e
                     WHERE e.TenantId = @TenantId AND e.IsDeleted = 0
                       AND e.HiredOn <= d.[Date]
                       AND (e.TerminatedOn IS NULL OR e.TerminatedOn > d.[Date])) AS decimal(18,2)) AS Value
            FROM Dates d
            ORDER BY d.[Date]
            OPTION (MAXRECURSION 400)
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value, From = from, To = to },
            cancellationToken: ct
        );

        IEnumerable<TimeSeriesPoint> result =
            await connection.QueryAsync<TimeSeriesPoint>(command);
        return result.AsList();
    }

    public async Task<LeaveStats> GetLeaveStatsAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        //? Two grouped result sets in one round trip via QueryMultiple.
        const string sql = """
            SELECT [Type] AS Label, COUNT(*) AS Value
            FROM [HrmSystem].[LeaveRequests]
            WHERE TenantId = @TenantId AND IsDeleted = 0
              AND PeriodStart <= @To AND PeriodEnd >= @From
            GROUP BY [Type];

            SELECT Status AS Label, COUNT(*) AS Value
            FROM [HrmSystem].[LeaveRequests]
            WHERE TenantId = @TenantId AND IsDeleted = 0
              AND PeriodStart <= @To AND PeriodEnd >= @From
            GROUP BY Status;
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value, From = from, To = to },
            cancellationToken: ct
        );

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(command);
        List<CategoryCount> byType = (await grid.ReadAsync<CategoryCount>()).AsList();
        List<CategoryCount> byStatus = (await grid.ReadAsync<CategoryCount>()).AsList();

        return new LeaveStats(byType, byStatus);
    }

    public async Task<IReadOnlyList<CategoryCount>> GetDepartmentDistributionAsync(
        TenantId tenantId,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT d.Name AS Label, COUNT(e.Id) AS Value
            FROM [HrmSystem].[Departments] d
            LEFT JOIN [HrmSystem].[Employees] e
                   ON e.DepartmentId = d.Id AND e.TenantId = @TenantId
                  AND e.IsDeleted = 0 AND e.Status <> 'Terminated'
            WHERE d.TenantId = @TenantId AND d.IsDeleted = 0
            GROUP BY d.Name
            ORDER BY Value DESC
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value },
            cancellationToken: ct
        );

        IEnumerable<CategoryCount> result = await connection.QueryAsync<CategoryCount>(command);
        return result.AsList();
    }

    //? Dapper row shape for the attendance pivot — private to this query class.
    private sealed record AttendanceTrendRow(DateOnly Date, int Present, int Late, int Absent);
}
