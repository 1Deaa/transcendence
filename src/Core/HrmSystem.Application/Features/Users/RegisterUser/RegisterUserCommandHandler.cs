using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, AccessTokensResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IIdentityService identityService,
        IJwtTokenProvider tokenProvider,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork
    )
    {
        _identityService = identityService;
        _tokenProvider = tokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AccessTokensResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken
    )
    {
        /*
            //?     Step 1 — domain validation first, before any Infrastructure call.
            //?     [User.Create] collects ALL broken rules in one pass (no fail-fast).
        */
        Result<User> userResult = User.Create(
            userName: command.UserName,
            firstName: command.FirstName,
            middleName: command.MiddleName,
            lastName: command.LastName,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            secondaryEmail: command.SecondaryEmail,
            secondaryPhoneNumber: command.SecondaryPhoneNumber
        );

        if (userResult.IsFailure)
        {
            return userResult.Errors;
        }

        /*
            //?     Step 2 — atomic persistence.
            //?     [RegisterAsync] owns the cross-context transaction:
            //?       1. Creates the ASP.NET Identity user
            //?       2. Links [IdentityId] onto the domain [User]
            //?       3. Persists the domain [User]
            //?       4. Commits both contexts in a single shared transaction
            //!     After success, [userResult.Value.IdentityId] is populated.
        */
        Result<UserId> registerResult = await _identityService.RegisterAsync(
            userResult.Value,
            command.Password,
            cancellationToken
        );

        if (registerResult.IsFailure)
        {
            return registerResult.Errors;
        }

        /*
            //?     Step 3 — create and persist the [RefreshToken] domain entity.
            //?     [RefreshToken.Create] generates the opaque secret internally via [RefreshTokenValue.New()].
            //?     The expiry window comes from [IJwtTokenProvider.RefreshTokenLifetime] so it is
            //?     always in sync with the JWT settings — one place to configure, zero divergence.
            //!     [IUnitOfWork.SaveChangesAsync] commits the refresh token AFTER the user transaction
            //!     succeeds. The two saves are intentionally separate: the user is the atomic unit;
            //!     a missing refresh token is recoverable (the user can log in again).
        */
        User user = userResult.Value;

        DateTimeOffset refreshExpiry = DateTimeOffset.UtcNow.Add(
            _tokenProvider.RefreshTokenLifetime
        );
        Result<RefreshToken> refreshTokenResult = RefreshToken.Create(user.Id!, refreshExpiry);

        if (refreshTokenResult.IsFailure)
        {
            return refreshTokenResult.Errors;
        }

        await _refreshTokenRepository.AddAsync(refreshTokenResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        /*
            //?     Step 4 — generate the JWT access token.
            //?     [CreateAccessToken] is a pure cryptographic operation — no DB access.
            //>     Auto-login: the user receives both tokens immediately after registration.
        */
        /*
            //!     New users have no roles assigned at registration.
            //!     Roles and permissions are granted by an admin after account creation.
            //!     The issued JWT has empty role/permission claims — it only proves identity.
        */
        (string accessToken, DateTime expiresOnUtc) = _tokenProvider.CreateAccessToken(
            new GenerateTokenRequest(
                IdentityUserId: user.IdentityId,
                DomainUserId: user.Id!.Value,
                Email: user.Email.Value,
                UserName: user.UserName.Value,
                PhoneNumber: user.PhoneNumber?.Value,
                Roles: [],
                Permissions: [],
                TenantId: user.TenantId?.Value
            )
        );

        return new AccessTokensResponse(
            accessToken,
            refreshTokenResult.Value.Token.Value,
            expiresOnUtc
        );
    }
}
