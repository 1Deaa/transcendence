using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.GetLeaveStats;

public sealed record GetLeaveStatsQuery(string TenantId, DateOnly From, DateOnly To)
    : ICachedQuery<LeaveStats>
{
    public string CacheKey => $"analytics:{TenantId}:leave-stats:{From:yyyyMMdd}:{To:yyyyMMdd}";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
