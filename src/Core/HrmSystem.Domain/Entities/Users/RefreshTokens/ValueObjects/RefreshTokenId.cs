using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of a RefreshToken aggregate.
    //
    //?     Format: "refresh-{UUIDv7}"
    //?       - The "refresh-" prefix makes IDs self-describing in logs and across service boundaries.
    //?       - UUIDv7 embeds a millisecond timestamp, making IDs naturally ordered by creation time.
    //
    //?     Two factories — intentionally different contracts:
    //?       - [New()]  → always generates a valid ID; no validation needed.
    //?       - [From()] → reconstitutes from an external source (API input, messaging);
    //?                    returns [Result<T>] because external input is untrusted.
    //
    //!     The constructor is [private] — use [New()] or [From()] to get an instance.
*/
public sealed record RefreshTokenId
{
    #region Constants (Prefix)

    private const string Prefix = "refresh-";

    #endregion


    #region Properties

    //? The full identifier string, e.g. "refresh-019750ab-...".
    public string Value { get; }

    #endregion


    #region Constructor

    private RefreshTokenId(string value)
    {
        Value = value;
    }

    #endregion


    #region Factories

    //? Generates a fresh, guaranteed-valid RefreshTokenId. Format: "refresh-{Guid.CreateVersion7()}".
    public static RefreshTokenId New() =>
        new RefreshTokenId(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    /*
        //?     Reconstitutes a [RefreshTokenId] from an externally supplied string
        //?     (e.g., an API route parameter or a message payload).
        //!     Returns a validation error when value is null, whitespace, or has the wrong prefix.
    */
    public static Result<RefreshTokenId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RefreshTokenErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return RefreshTokenErrors.Id.InvalidFormat;
        }

        return new RefreshTokenId(value);
    }

    #endregion
}
