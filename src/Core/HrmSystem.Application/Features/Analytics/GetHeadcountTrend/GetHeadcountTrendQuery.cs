using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.GetHeadcountTrend;

public sealed record GetHeadcountTrendQuery(string TenantId, DateOnly From, DateOnly To)
    : ICachedQuery<IReadOnlyList<TimeSeriesPoint>>
{
    public string CacheKey =>
        $"analytics:{TenantId}:headcount-trend:{From:yyyyMMdd}:{To:yyyyMMdd}";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
