using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.RefreshTokens;

namespace HrmSystem.Application.Features.Users.RefreshUserToken;

internal sealed class RefreshUserTokenCommandHandler
    : ICommandHandler<RefreshUserTokenCommand, AccessTokensResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshUserTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenProvider tokenProvider,
        IIdentityService identityService,
        IUnitOfWork unitOfWork
    )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenProvider = tokenProvider;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AccessTokensResponse>> Handle(
        RefreshUserTokenCommand command,
        CancellationToken cancellationToken
    )
    {
        /*
            //?     Step 1 — find the token row by its opaque value.
            //?     [FindByTokenValueAsync] uses the normal query (global filter [!IsDeleted] applies)
            //?     so we see Active tokens AND Superseded tokens — both have [IsDeleted = false].
            //?     Soft-deleted rows are invisible: if cleanup ran, the token is simply gone.
            //
            //!     Return the SAME opaque [NotValid] for every non-attack failure branch
            //!     (null, expired, explicitly revoked) — clients must not learn token state.
        */
        RefreshToken? token = await _refreshTokenRepository.FindByTokenValueAsync(
            command.RefreshToken,
            cancellationToken
        );

        if (token is null)
        {
            return RefreshTokenErrors.NotValid;
        }

        /*
            //!     ── REUSE DETECTION ──────────────────────────────────────────────────
            //?
            //?     [IsSuperseded = true] means this token was already rotated — the
            //?     holder received a new token at that time. If the old value reappears,
            //?     one copy was stolen (or the legitimate client has a serious bug).
            //?
            //?     [Response]: revoke EVERY active session for this user immediately.
            //?       - The legitimate(شرعي وقانوني) user loses their session — forced re-login.
            //?       - The attacker's copy is also invalidated.
            //?       - Both parties are out; security wins over convenience (راحة).
            //?
            //?     The error message is intentionally vague — revealing "reuse detected"
            //?     tells an attacker exactly when to act to avoid detection next time.
        */
        if (token.IsSuperseded)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(token.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return RefreshTokenErrors.PossibleCompromise;
        }

        if (!token.IsValid)
        {
            return RefreshTokenErrors.NotValid;
        }

        /*
            //!     Step 2 — load the domain [User] for claims.
            //?     [GetByIdAsync] applies global query filters — a suspended ([IsActive = false])
            //?     or soft-deleted user cannot refresh. Return [NotFound] for both.
        */
        User? user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        /*
            //!     Step 3 — rotate by superseding the current token and creating a new one.
            //?
            //?     Why NOT in-place update (the old [Rotate()] approach):
            //?       - In-place overwrites the secret value → old value vanishes from DB
            //?       - If the old value is resubmitted, [FindByTokenValueAsync] returns null
            //?       - We have no way to know WHO submitted it → reuse detection impossible
            //?
            //?     The supersede-and-create approach keeps the old row as a tripwire:
            //?       - Old row: same value, [IsSuperseded = true], findable
            //?       - New row: fresh value, [IsSuperseded = false], active
            //?       - If old value is submitted again → reuse detected (see Step 1)
            //
            //!     Both mutations (supersede + insert) are committed in a SINGLE
            //!     [SaveChangesAsync] call — they succeed or fail atomically.
            //!     No window exists where the old token is superseded but the new one
            //!     has not been persisted yet.
        */
        token.Supersede();

        DateTimeOffset newExpiry = DateTimeOffset.UtcNow.Add(_tokenProvider.RefreshTokenLifetime);
        Result<RefreshToken> newTokenResult = RefreshToken.Create(token.UserId, newExpiry);

        if (newTokenResult.IsFailure)
        {
            return newTokenResult.Errors;
        }

        await _refreshTokenRepository.AddAsync(newTokenResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        /*
            //?     Step 4 — re-load roles and permissions so the new token reflects any
            //?     role changes made by an admin since the previous token was issued.
            //?     Refresh is the one natural moment to pick up permission changes without
            //?     forcing the user to log out and back in.
        */
        IReadOnlyList<string> roles = await _identityService.GetRolesAsync(
            user.IdentityId,
            cancellationToken
        );

        IReadOnlyList<string> permissions = await _identityService.GetPermissionsAsync(
            user.IdentityId,
            cancellationToken
        );

        (string accessToken, DateTime expiresOnUtc) = _tokenProvider.CreateAccessToken(
            new GenerateTokenRequest(
                IdentityUserId: user.IdentityId,
                DomainUserId: user.Id!.Value,
                Email: user.Email.Value,
                UserName: user.UserName.Value,
                PhoneNumber: user.PhoneNumber?.Value,
                Roles: roles,
                Permissions: permissions,
                TenantId: user.TenantId?.Value
            )
        );

        return new AccessTokensResponse(
            accessToken,
            newTokenResult.Value.Token.Value,
            expiresOnUtc
        );
    }
}
