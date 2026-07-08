using HrmSystem.Application.Common.Authentication;

namespace HrmSystem.Application.Common.Interfaces.Authentication;

/*
    //?     Pure access-token generation contract — no database, no refresh-token logic.
    //?     Receives a trusted [GenerateTokenRequest] and produces a signed JWT + its expiry.
    //
    //*     Separated from refresh-token lifecycle intentionally:
    //*       - [IJwtTokenProvider]        — pure function: given a trusted identity, sign a JWT.
    //*       - [IIdentityService]         — orchestrates the full auth flow (register, login, roles).
    //*       - [IRefreshTokenRepository]  — persistence and lookup of [RefreshToken] entities.
    //
    //*     [RefreshTokenLifetime] is exposed here so Application-layer handlers can compute
    //*     the refresh-token expiry without depending on Infrastructure configuration types.
    //
    //!     [CreateAccessToken] never touches the database — it is a pure cryptographic operation.
    //!     The caller is responsible for ensuring the identity is already validated.
*/
public interface IJwtTokenProvider
{
    /*
        //?     Signs and returns a JWT access token together with its exact expiry timestamp.
        //?     [ExpiresOnUtc] is computed once inside this method — never recompute it at
        //?     the call site or the value in the response will drift from the JWT [exp] claim.
    */
    (string AccessToken, DateTime ExpiresOnUtc) CreateAccessToken(GenerateTokenRequest request);

    //*     How long a refresh token remains valid. Read from [JwtAuthOptions.RefreshTokenExpirationDays].
    TimeSpan RefreshTokenLifetime { get; }
}
