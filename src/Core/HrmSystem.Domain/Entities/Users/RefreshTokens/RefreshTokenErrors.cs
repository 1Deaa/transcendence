using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.RefreshTokens;

public static class RefreshTokenErrors
{
    // ── Aggregate-level ───────────────────────────────────────────────────────

    public static readonly Error NotFound = Error.NotFound(
        "RefreshToken.NotFound",
        "The refresh token was not found."
    );

    public static readonly Error Expired = Error.Conflict(
        "RefreshToken.Expired",
        "The refresh token has expired."
    );

    public static readonly Error Revoked = Error.Conflict(
        "RefreshToken.Revoked",
        "The refresh token has been revoked."
    );

    /*
        //!     Use [Invalid] when the token fails validation without revealing whether
        //!     it was not found, expired, or revoked — prevents timing-based state enumeration.
        //!     Named [Invalid] (not "Invalid") to avoid shadowing [Id.Invalid] (S3218).
    */
    public static readonly Error NotValid = Error.Unauthorized(
        "RefreshToken.NotValid",
        "The refresh token is not valid."
    );

    /*
        //!     Returned when a superseded (already-rotated) token is submitted again.
        //!     This is a security event — one copy of the token was stolen.
        //!     All sessions for this user have been invalidated as a precaution.
        //
        //!     The message is intentionally vague — do NOT reveal that reuse was detected.
        //!     Revealing the detection mechanism helps attackers time their attacks.
    */
    public static readonly Error PossibleCompromise = Error.Unauthorized(
        "RefreshToken.PossibleCompromise",
        "Your session has been invalidated for security reasons. Please sign in again."
    );

    // ── RefreshTokenId value object ───────────────────────────────────────────

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "RefreshToken.Id.Invalid",
            "The refresh token identifier is required and cannot be empty."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "RefreshToken.Id.InvalidFormat",
            "The refresh token identifier format is invalid. Expected format: \"refresh-{id}\"."
        );
    }

    // ── RefreshTokenValue value object ────────────────────────────────────────

    public static class Token
    {
        public static readonly Error Required = Error.Validation(
            "RefreshToken.Token.Required",
            "The token value is required and cannot be empty."
        );
    }

    // ── ExpiresOn ─────────────────────────────────────────────────────────────

    public static class ExpiresOn
    {
        public static readonly Error MustBeInFuture = Error.Validation(
            "RefreshToken.ExpiresOn.MustBeInFuture",
            "The expiration date must be in the future."
        );
    }
}
