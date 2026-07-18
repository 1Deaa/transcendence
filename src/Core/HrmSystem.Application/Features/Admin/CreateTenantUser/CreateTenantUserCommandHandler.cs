using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Admin.CreateTenantUser;

/*
    //?     The new account is ALWAYS created inside the caller's own workspace — the
    //?     tenant comes from the ambient ITenantContext, never from client input.
*/
internal sealed class CreateTenantUserCommandHandler(
    IIdentityService identityService,
    ITenantContext tenantContext
) : ICommandHandler<CreateTenantUserCommand, string>
{
    public async Task<Result<string>> Handle(
        CreateTenantUserCommand command,
        CancellationToken cancellationToken
    )
    {
        TenantId? tenantId = tenantContext.Current;
        if (tenantId is null)
        {
            return AdminErrors.TenantUnresolved;
        }

        if (!RoleNames.TenantAssignable.Contains(command.Role))
        {
            return AdminErrors.RoleNotAssignable;
        }

        Result<User> userResult = User.Create(
            userName: command.UserName,
            firstName: command.FirstName,
            middleName: null,
            lastName: command.LastName,
            email: command.Email
        );
        if (userResult.IsFailure)
        {
            return userResult.Errors;
        }

        User user = userResult.Value;
        user.AssignToTenant(tenantId);

        Result<UserId> registerResult = await identityService.RegisterAsync(
            user,
            command.Password,
            cancellationToken
        );
        if (registerResult.IsFailure)
        {
            return registerResult.Errors;
        }

        Result roleResult = await identityService.AddToRoleAsync(
            user.IdentityId,
            command.Role,
            cancellationToken
        );
        if (roleResult.IsFailure)
        {
            return roleResult.Errors;
        }

        return user.Id!.Value;
    }
}
