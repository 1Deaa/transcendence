using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.GetLeaveStats;

internal sealed class GetLeaveStatsQueryHandler(IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetLeaveStatsQuery, LeaveStats>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;

    public async Task<Result<LeaveStats>> Handle(
        GetLeaveStatsQuery query,
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

        LeaveStats stats = await _analyticsQueries.GetLeaveStatsAsync(
            tenantIdResult.Value,
            query.From,
            query.To,
            cancellationToken
        );

        return stats;
    }
}
