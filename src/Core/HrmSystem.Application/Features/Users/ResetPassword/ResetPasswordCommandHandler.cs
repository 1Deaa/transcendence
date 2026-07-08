using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;

namespace HrmSystem.Application.Features.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IIdentityService identityService
    )
    {
        _userRepository = userRepository;
        _identityService = identityService;
    }

    public async Task<Result> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken
    )
    {
        /*
            //?     Find the user by email. [FindByIdentifierAsync] searches Email, UserName,
            //?     and PhoneNumber — email is always unique so this is a deterministic match.
            //
            //!     If the user is not found, return [InvalidToken] — not [NotFound].
            //!     The token was embedded in a link the user clicked, so returning "user not found"
            //!     would reveal the email is not registered (enumeration risk).
        */
        User? user = await _userRepository.FindByAnyIdentifierAsync(
            command.Email,
            cancellationToken
        );

        if (user is null)
        {
            return UserErrors.InvalidToken;
        }

        /*
            //?     Delegates to [UserManager.ResetPasswordAsync] which validates the token
            //?     cryptographically, checks expiry, and re-hashes the new password.
            //!     Returns [UserErrors.InvalidToken] if the token is expired/invalid, or
            //!     Identity Validation errors if the new password violates the policy.
        */
        return await _identityService.ResetPasswordAsync(
            user.IdentityId,
            command.Token,
            command.NewPassword,
            cancellationToken
        );
    }
}
