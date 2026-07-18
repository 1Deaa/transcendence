using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Admin.ChangeUserRole;

internal sealed class ChangeUserRoleCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext
) : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<Result> Handle(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!RoleNames.TenantAssignable.Contains(command.Role))
        {
            return AdminErrors.RoleNotAssignable;
        }

        Result<UserId> userIdResult = UserId.From(command.UserId);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Errors.ToList();
        }

        //! Self-demotion guard: an admin locking themselves out is a support ticket, not a feature.
        if (currentUserContext.DomainUserId?.Value == userIdResult.Value.Value)
        {
            return AdminErrors.CannotModifySelf;
        }

        User? user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);

        /*
            //!     Cross-tenant probe defense: a user outside the caller's workspace is
            //!     reported as NOT FOUND — never as "exists but foreign".
        */
        bool userInWorkspace =
            user is not null
            && user.TenantId is not null
            && user.TenantId.Value == tenantContext.Current?.Value;

        if (!userInWorkspace)
        {
            return AdminErrors.UserNotFound;
        }

        return await identityService.SetRoleAsync(
            user!.IdentityId,
            command.Role,
            cancellationToken
        );
    }
}
