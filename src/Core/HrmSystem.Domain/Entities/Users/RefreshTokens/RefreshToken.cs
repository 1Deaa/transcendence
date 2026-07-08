using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Domain.Entities.Users.RefreshTokens;

/*
    //?     Refresh token issued to a user after a successful authentication.
    //?     Long-lived opaque credential exchanged for a new access token.
    //
    //*     State machine — four distinct states:
    //*       Active      IsActive=true,  IsSuperseded=false → can be used for a token refresh
    //*       Superseded  IsActive=true,  IsSuperseded=true  → was rotated; kept as a reuse tripwire
    //*       Revoked     IsActive=false, IsSuperseded=any   → explicitly invalidated (logout / security)
    //*       Cleaned up  IsDeleted=true                     → swept by background job, gone from normal queries
    //
    //!     [IsValid] is the ONLY gate before issuing a new access token — check nothing else.
    //
    //!     [IsSuperseded] is intentionally NOT the same as [IsRevoked]:
    //!       - Superseded = this token was legitimately rotated; the holder got a new one
    //!       - If it reappears, one copy was stolen → security event → revoke all sessions
    //!     Never set [IsSuperseded] outside [Supersede()] — domain invariant.
*/
public sealed class RefreshToken : AFullAuditableEntity<RefreshTokenId>
{
    #region Constructor

    //! Parameterless constructor: EF Core materialisation only — never call from domain code.
    private RefreshToken()
        : base() { }

    private RefreshToken(
        RefreshTokenId id,
        RefreshTokenValue token,
        DateTimeOffset expiresOn,
        UserId userId
    )
        : base(id)
    {
        Token = token;
        ExpiresOn = expiresOn;
        UserId = userId;
    }

    #endregion

    #region Properties

    public RefreshTokenValue Token { get; private set; } = null!;
    public DateTimeOffset ExpiresOn { get; private set; }
    public bool IsSuperseded { get; private set; }

    /*
        //?     [IsExpired]    — wall-clock comparison, never cached.
        //?     [IsRevoked]    — alias for [!IsActive]; set by [Revoke()].
        //?     [IsSuperseded] — set by [Supersede()]; the token was rotated.
        //?     [IsValid]      — ALL three gates must pass before a refresh is allowed.
    */
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresOn;
    public bool IsRevoked => !IsActive;
    public bool IsValid => !IsExpired && !IsRevoked && !IsSuperseded;

    #endregion

    #region Navigation Properties

    public UserId UserId { get; private set; } = null!;
    public User User { get; private set; } = null!;

    #endregion

    #region Factory

    /*
        //?     The only way to produce a valid [RefreshToken].
        //?     [RefreshTokenValue.New()] generates the opaque secret internally —
        //?     the caller never supplies raw entropy.
        //!     [expiresOn] must be in the future; the caller computes the window
        //!     using [IJwtTokenProvider.RefreshTokenLifetime].
    */
    public static Result<RefreshToken> Create(UserId userId, DateTimeOffset expiresOn)
    {
        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiresOn.MustBeInFuture;
        }

        return new RefreshToken(RefreshTokenId.New(), RefreshTokenValue.New(), expiresOn, userId);
    }

    #endregion

    #region Methods

    /*
        //*     Marks this token as superseded after a successful rotation.
        //*     The row is intentionally kept in the database:
        //*       - If the same value is submitted again, it proves one copy was stolen.
        //*       - The handler detects [IsSuperseded = true] and triggers [RevokeAllForUserAsync].
        //!     Do NOT call [MarkAsDeleted()] after superseding — the row must stay
        //!     queryable for reuse detection. Background cleanup runs after a safe window.
        //!     The caller must create a NEW [RefreshToken] via [Create()] and persist it.
    */
    //! Rotate() (in-place overwrite) → Supersede() (old row stays, new row created).
    public void Supersede() => IsSuperseded = true;

    /*
        //*     Explicitly revokes the token — used on logout or security events.
        //*     Delegates to [AFullAuditableEntity.Deactivate()] so [ModifiedAt] is
        //*     updated by the audit interceptor on the next [SaveChanges].
        //!     Distinct from [Supersede()]: a revoked token was invalidated deliberately,
        //!     not as part of rotation. Resubmitting it does NOT trigger a security alert.
    */
    public void Revoke() => Deactivate();

    #endregion
}
