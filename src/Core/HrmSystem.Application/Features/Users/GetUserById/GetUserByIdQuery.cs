using HrmSystem.Application.Common.Interfaces.Caching;

namespace HrmSystem.Application.Features.Users.GetUserById;

/*
    //?     Carries the raw [userId] string from the route segment — e.g. "user-019750ab-...".
    //?     The handler parses it into a [UserId] value object and returns a validation error
    //?     if the format is wrong.  The controller never touches [UserId].
    //
    //*     Shared by:  API [GET /api/users/{userId}]  and  MVC Admin [GET /admin/users/{userId}]
    //
    //*     Implements [ICachedQuery] so [QueryCachingBehavior] intercepts it automatically.
    //*     Cache key uses the raw ID string which already contains the "user-" prefix.
    //
    //!     Cache key prefix "users:profile:" is distinct from "users:me:" ([GetCurrentUserQuery])
    //!     — both cache the same user's data but as different response types.
    //!     If [RawUserId] is malformed, the handler returns a validation error and
    //!     [QueryCachingBehavior] never stores it (only caches on [IsSuccess]).
*/
public sealed record GetUserByIdQuery(string RawUserId)
    : ICachedQuery<UserProfileResponse>
{
    public string CacheKey => $"users:profile:{RawUserId}";

    //? 10 minutes — same window as [GetCurrentUserQuery] for consistency.
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
