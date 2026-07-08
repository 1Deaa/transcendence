using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Users.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand>
{
    private readonly IIdentityService _identityService;

    public ConfirmEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        /*
            //?     Delegates directly to [UserManager.ConfirmEmailAsync].
            //?     No domain repository lookup needed — the confirmation token is purpose-scoped
            //?     to the Identity user ID and cannot be used for another user or another purpose.
            //
            //!     Returns [UserErrors.InvalidToken] when:
            //!       - The Identity user does not exist (link contained a garbage ID).
            //!       - The token is malformed, expired, or was already consumed.
        */
        return await _identityService.ConfirmEmailAsync(
            command.IdentityUserId,
            command.Token,
            cancellationToken
        );
    }
}
