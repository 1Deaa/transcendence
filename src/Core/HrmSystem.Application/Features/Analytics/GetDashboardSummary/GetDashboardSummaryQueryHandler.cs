using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.GetDashboardSummary;

internal sealed class GetDashboardSummaryQueryHandler(
    IAnalyticsQueries analyticsQueries,
    IDateTimeProvider dateTimeProvider
) : IQueryHandler<GetDashboardSummaryQuery, KpiSummary>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<Result<KpiSummary>> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<TenantId> tenantIdResult = TenantId.From(query.TenantId);
        if (tenantIdResult.IsFailure)
        {
            return tenantIdResult.Errors.ToList();
        }

        KpiSummary summary = await _analyticsQueries.GetDashboardSummaryAsync(
            tenantIdResult.Value,
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            cancellationToken
        );

        return summary;
    }
}
