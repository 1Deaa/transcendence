using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Users.LoginUser;

internal sealed class LogInUserCommandHandler
    : ICommandHandler<LogInUserCommand, AccessTokensResponse>
{
    private readonly IIdentityService _identityService;

    public LogInUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result<AccessTokensResponse>> Handle(
        LogInUserCommand command,
        CancellationToken cancellationToken
    )
    {
        return _identityService.LoginAsync(command.Identifier, command.Password, cancellationToken);
    }
}
