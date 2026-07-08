using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.GetDashboardSummary;

/*
    //?     KPI tiles query — Redis-cached 60s per tenant.
    //!     TenantId is part of the record (stamped by the controller from the caller's claim)
    //!     PRECISELY so the cache key is tenant-scoped — a shared key would leak KPIs across tenants.
*/
public sealed record GetDashboardSummaryQuery(string TenantId) : ICachedQuery<KpiSummary>
{
    public string CacheKey => $"analytics:{TenantId}:summary";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
