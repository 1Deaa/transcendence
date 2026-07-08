using System.Security.Cryptography;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;

/*
    //?     Opaque, cryptographically random token string used as the refresh secret.
    //?     Not a JWT — it is an opaque bearer credential validated by database lookup,
    //?     not by signature verification. The caller never supplies the raw entropy.
    //
    //*     [New()] generates 32 random bytes (256-bit) encoded as Base64 —
    //*     44 characters, URL-safe, standard padding included.
    //
    //!     The constructor is [private] — use [New()] or [From()] to get an instance.
    //!     [From()] is for reconstitution from trusted storage (DB read) only.
    //!     Never accept a raw token string from user input without a constant-time comparison.
*/
public sealed record RefreshTokenValue
{
    #region Constructor

    private RefreshTokenValue(string value) => Value = value;

    #endregion


    #region Properties

    /*
        //?     [ByteLength] is the source-of-truth for how many random bytes [New()] generates.
        //?     Base64 encoding: ceil(ByteLength / 3) × 4 = ceil(32 / 3) × 4 = 44 characters.
        //*     Exposed as constants so the EF Core column configuration can size the column precisely.
    */
    public const int ByteLength = 32;
    public const int StorageLength = (ByteLength + 2) / 3 * 4; // = 44 for Base64 of 32 bytes

    public string Value { get; }

    #endregion


    #region Factory methods

    //? Generates a fresh cryptographically random token. Always valid — no Result needed.
    public static RefreshTokenValue New()
    {
        string raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(ByteLength));
        return new RefreshTokenValue(raw);
    }

    /*
        //?     Reconstitutes a [RefreshTokenValue] from a stored string (e.g., a DB column read).
        //!     Does not re-validate entropy or length — trusts the storage layer wrote a value
        //!     originally produced by [New()]. Only guards against null / empty / whitespace.
    */
    public static Result<RefreshTokenValue> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RefreshTokenErrors.Token.Required;
        }

        return new RefreshTokenValue(value);
    }

    #endregion
}
