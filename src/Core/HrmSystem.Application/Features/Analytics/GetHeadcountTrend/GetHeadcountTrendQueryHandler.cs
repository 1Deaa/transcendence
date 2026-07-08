using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.GetHeadcountTrend;

internal sealed class GetHeadcountTrendQueryHandler(IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetHeadcountTrendQuery, IReadOnlyList<TimeSeriesPoint>>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;

    public async Task<Result<IReadOnlyList<TimeSeriesPoint>>> Handle(
        GetHeadcountTrendQuery query,
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

        IReadOnlyList<TimeSeriesPoint> points = await _analyticsQueries.GetHeadcountTrendAsync(
            tenantIdResult.Value,
            query.From,
            query.To,
            cancellationToken
        );

        return Result.Success(points);
    }
}
