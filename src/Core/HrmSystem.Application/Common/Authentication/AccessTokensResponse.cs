namespace HrmSystem.Application.Common.Authentication;

/*
    //?     The token pair returned after a successful register or login.
    //
    //*     [AccessToken]  — short-lived JWT, sent in the Authorization: Bearer header.
    //*     [RefreshToken] — long-lived opaque token, used to rotate the access token silently.
    //*     [ExpiresOnUtc] — computed once inside token generation and passed through — never
    //*                      recomputed so the value always matches what was embedded in the JWT.
*/
public sealed record AccessTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresOnUtc
);
