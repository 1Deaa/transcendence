using Microsoft.AspNetCore.Authorization;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;

namespace HrmSystem.Web.Authentication;

/*
    //?     Second handler for [PermissionAuthorizationRequirement].
    //?     ASP.NET Core runs ALL registered [IAuthorizationHandler] implementations for a
    //?     requirement — so both this and [PermissionAuthorizationHandler] execute on every
    //?     [HasPermission("habits:read")] check.
    //
    //*     Division of responsibility:
    //*       - [PermissionAuthorizationHandler]  → grants access when the claim is present.
    //*       - [BlockedUserAuthorizationHandler] → vetoes when the account is deactivated.
    //
    //!     Never calls [context.Succeed] — this handler only vetoes.
    //!     [context.Fail] is a hard veto: it overrides any [Succeed] from other handlers.
    //!     Registered as [Scoped] because [IIdentityService] is Scoped (owns UserManager).
*/
public sealed class BlockedUserAuthorizationHandler
    : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IIdentityService _identityService;

    public BlockedUserAuthorizationHandler(IIdentityService identityService) =>
        _identityService = identityService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement
    )
    {
        string? identityUserId = context.User.FindFirst(CustomClaimTypes.Sub)?.Value;

        if (string.IsNullOrEmpty(identityUserId))
        {
            return;
        }

        bool isActive = await _identityService.IsActiveAsync(
            identityUserId,
            CancellationToken.None
        );

        if (!isActive)
        {
            context.Fail(
                new AuthorizationFailureReason(this, "Account is blocked and deactivated.")
            );
        }
    }
}
