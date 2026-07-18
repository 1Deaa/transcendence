using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Admin.SetUserActivation;

/*
    //?     Deactivation is enforced by [BlockedUserAuthorizationHandler] on every request —
    //?     a suspended account fails authorization even with a still-valid JWT.
*/
internal sealed class SetUserActivationCommandHandler(
    IUserRepository userRepository,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetUserActivationCommand>
{
    public async Task<Result> Handle(
        SetUserActivationCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<UserId> userIdResult = UserId.From(command.UserId);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Errors.ToList();
        }

        //! Self-suspension guard — an admin locking themselves out helps nobody.
        if (currentUserContext.DomainUserId?.Value == userIdResult.Value.Value)
        {
            return AdminErrors.CannotModifySelf;
        }

        User? user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);

        bool userInWorkspace =
            user is not null
            && user.TenantId is not null
            && user.TenantId.Value == tenantContext.Current?.Value;

        if (!userInWorkspace)
        {
            return AdminErrors.UserNotFound;
        }

        if (command.IsActive)
        {
            user!.Activate();
        }
        else
        {
            user!.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
