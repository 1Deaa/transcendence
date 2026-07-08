using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? The strongly-typed, lexicographically-sortable identity of a User aggregate.
 *
 *? Format: "user-{UUIDv7}"
 *?   - The "user-" prefix makes IDs self-describing in logs and across service boundaries.
 *?   - UUIDv7 embeds a millisecond timestamp, making IDs naturally ordered by creation time.
 *
 *? Two factories — intentionally different contracts:
 *?   - New()  → always generates a valid ID; no validation needed.
 *?   - From() → reconstitutes from an external source (API input, messaging);
 *?              returns Result<T> because external input is untrusted.
 *
 *! The constructor is [private] — use New() or From() to get an instance.
 */
public sealed record UserId
{
    #region Constants (Prefix)

    private const string Prefix = "user-";

    #endregion


    #region Properties

    //? The full identifier string, e.g. "user-019750ab-...".
    public string Value { get; }

    #endregion


    #region Constructor

    private UserId(string value)
    {
        Value = value;
    }

    #endregion Constructor


    #region Factories

    //? Generates a fresh, guaranteed-valid UserId. Format: "user-{Guid.CreateVersion7()}".
    public static UserId New() =>
        new UserId(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    /*
     *? Reconstitutes a UserId from an externally supplied string
     *?  (e.g., an API route parameter or a message payload).
     *! Returns a validation error when value is null, whitespace, or has the wrong prefix.
     */
    public static Result<UserId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.Id.Invalid;
        }
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return UserErrors.Id.InvalidFormat;
        }
        return new UserId(value);
    }

    #endregion Factories
}
