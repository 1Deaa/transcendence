using System.Security.Claims;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace HrmSystem.Infrastructure.Services.Identity;

/*
    //*     Implements [ICurrentUserContext] by reading claims from [IHttpContextAccessor].
    //*     Works for both authentication schemes because [MvcLoginCommandHandler],
    //*     [MvcRegisterCommandHandler], and [JwtTokenProvider] all stamp the same
    //*     [CustomClaimTypes.*] constants on their respective principals.
    //
    //!     Registered as [Scoped] — one instance per HTTP request.
    //!     Never cache the [ClaimsPrincipal]: it is resolved fresh on each property access
    //!     so middleware changes to [HttpContext.User] are always reflected.
*/
internal sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /*
        //?     Parses the [domain_sub] claim into a [UserId] VO.
        //?     Returns [null] when the claim is absent or contains a value that does not
        //?     match the "user-{uuid}" format — treats both cases as unauthenticated.
        //!     This is the primary key used to load the domain [User] — prefer it over
        //!     [IdentityId] for any Application/Domain layer lookup.
    */
    public UserId? DomainUserId
    {
        get
        {
            string? domainUserId = Principal?.FindFirstValue(CustomClaimTypes.DomainUserId);
            if (domainUserId is null)
            {
                return null;
            }

            Result<UserId> userIdResult = UserId.From(domainUserId);
            return userIdResult.IsFailure ? null : userIdResult.Value;
        }
    }

    //? ASP.NET Identity PK — from the standard [sub] claim.
    public string? IdentityId => Principal?.FindFirstValue(CustomClaimTypes.Sub);

    //? Primary email — from the [email] claim.
    public string? Email => Principal?.FindFirstValue(CustomClaimTypes.Email);

    //? Display / login username — from the [preferred_username] claim.
    public string? UserName => Principal?.FindFirstValue(CustomClaimTypes.PreferredUsername);

    //? Phone number — from the [phone_number] claim; null when not in token.
    public string? PhoneNumber => Principal?.FindFirstValue(CustomClaimTypes.PhoneNumber);
}
