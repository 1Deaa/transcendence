using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Analytics.GetDepartmentDistribution;

internal sealed class GetDepartmentDistributionQueryHandler(IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetDepartmentDistributionQuery, IReadOnlyList<CategoryCount>>
{
    private readonly IAnalyticsQueries _analyticsQueries = analyticsQueries;

    public async Task<Result<IReadOnlyList<CategoryCount>>> Handle(
        GetDepartmentDistributionQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<TenantId> tenantIdResult = TenantId.From(query.TenantId);
        if (tenantIdResult.IsFailure)
        {
            return tenantIdResult.Errors.ToList();
        }

        IReadOnlyList<CategoryCount> distribution =
            await _analyticsQueries.GetDepartmentDistributionAsync(
                tenantIdResult.Value,
                cancellationToken
            );

        return Result.Success(distribution);
    }
}
