using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Analytics.ExportAnalyticsReport;
using HrmSystem.Application.Features.Analytics.GetAttendanceTrend;
using HrmSystem.Application.Features.Analytics.GetDashboardSummary;
using HrmSystem.Application.Features.Analytics.GetDepartmentDistribution;
using HrmSystem.Application.Features.Analytics.GetHeadcountTrend;
using HrmSystem.Application.Features.Analytics.GetLeaveStats;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Analytics;

/*
    //*     Analytics dashboard resources — every query is tenant-scoped.
    //?     The controller stamps the caller's TenantId (from the JWT claim, via ITenantContext)
    //?     into each query record so Redis cache keys are tenant-scoped by construction.
    //
    //>     Routes owned here:   GET /api/analytics/summary
    //>                          GET /api/analytics/attendance-trend?from=&to=&departmentId=
    //>                          GET /api/analytics/headcount-trend?from=&to=
    //>                          GET /api/analytics/leave-stats?from=&to=
    //>                          GET /api/analytics/department-distribution
    //>                          GET /api/analytics/reports/dashboard/export?format=pdf|csv&from=&to=
*/
[Route("api/analytics")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AnalyticsController(ISender sender, ITenantContext tenantContext)
    : ApiBaseController
{
    /// <summary>KPI tiles: headcount, today's attendance split, pending leaves, departments.</summary>
    [HttpGet("summary")]
    [HasPermission(Permissions.Analytics.Read)]
    [ProducesResponseType<KpiSummary>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<KpiSummary> result = await sender.Send(
            new GetDashboardSummaryQuery(tenantContext.Current.Value),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Present/Late/Absent series per day for the main dashboard chart.</summary>
    [HttpGet("attendance-trend")]
    [HasPermission(Permissions.Analytics.Read)]
    [ProducesResponseType<IReadOnlyList<ChartSeries>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceTrend(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? departmentId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<IReadOnlyList<ChartSeries>> result = await sender.Send(
            new GetAttendanceTrendQuery(tenantContext.Current.Value, from, to, departmentId),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Cumulative active headcount per day.</summary>
    [HttpGet("headcount-trend")]
    [HasPermission(Permissions.Analytics.Read)]
    [ProducesResponseType<IReadOnlyList<TimeSeriesPoint>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHeadcountTrend(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<IReadOnlyList<TimeSeriesPoint>> result = await sender.Send(
            new GetHeadcountTrendQuery(tenantContext.Current.Value, from, to),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Leave requests split by type and by status inside the window.</summary>
    [HttpGet("leave-stats")]
    [HasPermission(Permissions.Analytics.Read)]
    [ProducesResponseType<LeaveStats>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLeaveStats(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<LeaveStats> result = await sender.Send(
            new GetLeaveStatsQuery(tenantContext.Current.Value, from, to),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Active employees per department (donut chart).</summary>
    [HttpGet("department-distribution")]
    [HasPermission(Permissions.Analytics.Read)]
    [ProducesResponseType<IReadOnlyList<CategoryCount>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentDistribution(
        CancellationToken cancellationToken
    )
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<IReadOnlyList<CategoryCount>> result = await sender.Send(
            new GetDepartmentDistributionQuery(tenantContext.Current.Value),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Exports the dashboard report as a downloadable file (csv or pdf).</summary>
    [HttpGet("reports/dashboard/export")]
    [HasPermission(Permissions.Analytics.Export)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDashboardReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string format = "pdf",
        CancellationToken cancellationToken = default
    )
    {
        if (tenantContext.Current is null)
        {
            return Problem([AnalyticsErrors.TenantRequired]);
        }

        Result<ExportedReport> result = await sender.Send(
            new ExportAnalyticsReportQuery(tenantContext.Current.Value, from, to, format),
            cancellationToken
        );

        return result.Match<IActionResult>(
            report => File(report.Content, report.ContentType, report.FileName),
            Problem
        );
    }
}
