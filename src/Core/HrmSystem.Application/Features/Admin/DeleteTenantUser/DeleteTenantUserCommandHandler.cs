using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Admin.DeleteTenantUser;

internal sealed class DeleteTenantUserCommandHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteTenantUserCommand>
{
    public async Task<Result> Handle(
        DeleteTenantUserCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<UserId> userIdResult = UserId.From(command.UserId);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Errors.ToList();
        }

        //! Self-delete guard
        if (currentUserContext.DomainUserId?.Value == userIdResult.Value.Value)
        {
            return AdminErrors.CannotModifySelf;
        }

        User? user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);

        bool userInWorkspace =
            user is not null
            && user.TenantId is not null
            && user.TenantId.Value == tenantContext.Current?.Value;

        if (!userInWorkspace || user is null || user.Id is null)
        {
            return AdminErrors.UserNotFound;
        }

        await employeeRepository.DeletePermanentlyByEmailAsync(user.Email.Value, cancellationToken);
        await userRepository.DeletePermanentlyByIdAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
