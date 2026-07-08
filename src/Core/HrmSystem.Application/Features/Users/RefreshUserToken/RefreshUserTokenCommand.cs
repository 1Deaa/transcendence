using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.RefreshUserToken;

public sealed record RefreshUserTokenCommand(string RefreshToken) : ICommand<AccessTokensResponse>;
