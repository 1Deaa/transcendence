using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.GetAttendanceTrend;

//? Present/Late/Absent per day. Cache key carries every parameter — tenant first.
public sealed record GetAttendanceTrendQuery(
    string TenantId,
    DateOnly From,
    DateOnly To,
    string? DepartmentId
) : ICachedQuery<IReadOnlyList<ChartSeries>>
{
    public string CacheKey =>
        $"analytics:{TenantId}:attendance-trend:{From:yyyyMMdd}:{To:yyyyMMdd}:{DepartmentId ?? "all"}";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
