using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Queries;

/*
    //?     Dapper read queries behind the analytics dashboard.
    //!     ABSOLUTE RULE (CLAUDE.md): every method takes an explicit TenantId and every SQL
    //!     statement includes WHERE TenantId = @TenantId — Dapper bypasses the EF query
    //!     filters, so this parameter IS the tenant isolation.
*/
public interface IAnalyticsQueries
{
    //? KPI tiles: headcount, today's attendance split, pending leaves, department count.
    Task<KpiSummary> GetDashboardSummaryAsync(
        TenantId tenantId,
        DateOnly today,
        CancellationToken ct
    );

    //? Present/Late/Absent counts per day — the dashboard's main line/bar chart.
    Task<IReadOnlyList<ChartSeries>> GetAttendanceTrendAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        string? departmentId,
        CancellationToken ct
    );

    //? Cumulative active headcount per day (hires minus terminations).
    Task<IReadOnlyList<TimeSeriesPoint>> GetHeadcountTrendAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );

    //? Leave requests split by type and by status inside the window (pie/donut pair).
    Task<LeaveStats> GetLeaveStatsAsync(
        TenantId tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );

    //? Active employees per department (donut).
    Task<IReadOnlyList<CategoryCount>> GetDepartmentDistributionAsync(
        TenantId tenantId,
        CancellationToken ct
    );
}
