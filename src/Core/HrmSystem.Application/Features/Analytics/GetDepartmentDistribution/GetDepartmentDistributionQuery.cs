using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.GetDepartmentDistribution;

public sealed record GetDepartmentDistributionQuery(string TenantId)
    : ICachedQuery<IReadOnlyList<CategoryCount>>
{
    public string CacheKey => $"analytics:{TenantId}:department-distribution";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
