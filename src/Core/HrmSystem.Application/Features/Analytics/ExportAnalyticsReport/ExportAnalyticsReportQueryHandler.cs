using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Reporting;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.ExportAnalyticsReport;

/*
    //?     Gathers the dashboard data once, then delegates to the format-specific renderer
    //?     (csv / pdf) resolved by its Format key from the injected renderer set.
*/
internal sealed class ExportAnalyticsReportQueryHandler(
    IAnalyticsQueries analyticsQueries,
    ITenantRepository tenantRepository,
    IEnumerable<IAnalyticsReportRenderer> renderers,
    IDateTimeProvider dateTimeProvider
) : IQueryHandler<ExportAnalyticsReportQuery, ExportedReport>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly IEnumerable<IAnalyticsReportRenderer> _renderers = renderers;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<Result<ExportedReport>> Handle(
        ExportAnalyticsReportQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<TenantId> tenantIdResult = TenantId.From(query.TenantId);
        if (tenantIdResult.IsFailure)
        {
            return tenantIdResult.Errors.ToList();
        }

        if (query.From > query.To || query.To.DayNumber - query.From.DayNumber > 366)
        {
            return AnalyticsErrors.InvalidDateRange;
        }

        IAnalyticsReportRenderer? renderer = _renderers.FirstOrDefault(r =>
            string.Equals(r.Format, query.Format, StringComparison.OrdinalIgnoreCase)
        );
        if (renderer is null)
        {
            return AnalyticsErrors.UnsupportedFormat(query.Format);
        }

        Tenant? tenant = await _tenantRepository.GetByIdAsync(
            tenantIdResult.Value,
            cancellationToken
        );
        if (tenant is null)
        {
            return TenantErrors.NotFound;
        }

        KpiSummary summary = await _analyticsQueries.GetDashboardSummaryAsync(
            tenantIdResult.Value,
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            cancellationToken
        );
        IReadOnlyList<CategoryCount> distribution =
            await _analyticsQueries.GetDepartmentDistributionAsync(
                tenantIdResult.Value,
                cancellationToken
            );
        LeaveStats leaveStats = await _analyticsQueries.GetLeaveStatsAsync(
            tenantIdResult.Value,
            query.From,
            query.To,
            cancellationToken
        );

        var model = new AnalyticsReportModel(
            tenant.CompanyName.Value,
            query.From,
            query.To,
            summary,
            distribution,
            leaveStats
        );

        return renderer.Render(model);
    }
}
