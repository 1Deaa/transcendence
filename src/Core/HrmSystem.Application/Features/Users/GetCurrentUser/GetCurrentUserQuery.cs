using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Users.GetCurrentUser;

/*
    //?     The controller (presentation layer) resolves [DomainUserId] from [ICurrentUserContext]
    //?     and passes it here.  The handler never touches [ICurrentUserContext] — it only knows
    //?     "load the user with this ID", which is a pure application-layer concern.
    //
    //*     Shared by:  API [GET /api/users/me]  and  MVC Admin [GET /admin/profile]
    //*     Both presentation surfaces pass the same [DomainUserId] — scheme-agnostic.
    //
    //*     Implements [ICachedQuery] so [QueryCachingBehavior] intercepts it automatically.
    //*     Cache key is per-user: a cache hit for user A never returns data for user B.
    //
    //!     Cache key prefix "users:me:" is distinct from "users:profile:" used by
    //!     [GetUserByIdQuery] — both cache the same user but as different response types
    //!     ([CurrentUserResponse] vs [UserProfileResponse]).  Sharing a key would cause
    //!     deserialization failures on a cross-type cache hit.
*/
public sealed record GetCurrentUserQuery(UserId DomainUserId)
    : ICachedQuery<CurrentUserResponse>
{
    public string CacheKey => $"users:me:{DomainUserId.Value}";

    //? 10 minutes: long enough to absorb burst traffic; short enough to reflect role/permission
    //? changes (assigned by admin) without a manual cache flush.
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
