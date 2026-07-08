using Microsoft.AspNetCore.Authorization;
using HrmSystem.Application.Common.Authentication;

namespace HrmSystem.Web.Authentication;

/*
    //! A requirement [IAuthorizationRequirement] succeeds when at least one handler calls context.Succeed(requirement).
    //*     Evaluates a [PermissionAuthorizationRequirement] against the current [ClaimsPrincipal].
    //*     Succeeds when the principal carries at least one claim whose type is
    //*     [CustomClaimTypes.Permission] ("permission") and whose value matches
    //*     the required permission string exactly.
    //
    //*     Works identically for JWT principals (API) and Cookie principals (MVC):
    //*       - JWT:    [JwtTokenProvider] embeds permission claims when issuing the token.
    //*       - Cookie: [MvcLoginCommandHandler] stamps permission claims into the principal.
    //
    //!     The comparison is case-sensitive (Ordinal) — permission strings are always lowercase
    //!     by convention (see [Permissions] constants class).
    //!     A missing or mis-cased claim will NOT satisfy the requirement.
*/
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement
    )
    {
        bool hasPermission = context.User.Claims.Any(c =>
            c.Type == CustomClaimTypes.Permission && c.Value == requirement.Permission
        );

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
