using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Application.Features.Status.GetPublicStatus;

/*
    //?     ICachedQuery — the anonymous status page can be hammered; a 30-second Redis TTL
    //?     keeps snapshot-table load flat regardless of traffic.
    //!     Cache failure degrades gracefully: CacheService swallows Redis errors (cache miss),
    //!     so the status page stays reachable even while Redis itself is the incident.
*/
public sealed record GetPublicStatusQuery : ICachedQuery<PublicStatusResponse>
{
    public string CacheKey => "status:public";

    public TimeSpan? Expiration => TimeSpan.FromSeconds(30);
}
