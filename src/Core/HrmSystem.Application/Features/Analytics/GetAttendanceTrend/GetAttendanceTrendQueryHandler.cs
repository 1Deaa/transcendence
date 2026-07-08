using HrmSystem.Application.Common.Errors;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.GetAttendanceTrend;

internal sealed class GetAttendanceTrendQueryHandler(IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetAttendanceTrendQuery, IReadOnlyList<ChartSeries>>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;

    public async Task<Result<IReadOnlyList<ChartSeries>>> Handle(
        GetAttendanceTrendQuery query,
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

        IReadOnlyList<ChartSeries> series = await _analyticsQueries.GetAttendanceTrendAsync(
            tenantIdResult.Value,
            query.From,
            query.To,
            query.DepartmentId,
            cancellationToken
        );

        return Result.Success(series);
    }
}
