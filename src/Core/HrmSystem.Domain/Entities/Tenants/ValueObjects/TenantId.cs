using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Tenants.ValueObjects;

/*
    //?     The strongly-typed, lexicographically-sortable identity of a Tenant aggregate.
    //
    //?     Format: "tenant-{UUIDv7}"
    //?      - The "tenant-" prefix makes IDs self-describing in logs and across service boundaries.
    //?      - UUIDv7 embeds a millisecond timestamp, making IDs naturally ordered by creation time
    //?        without a separate CreatedAt sort column.
    //
    //?     Two factories — intentionally different contracts:
    //?      - New()  → always generates a valid ID; no validation needed.
    //?      - From() → reconstitutes from an external source (API input, JWT claim, messaging);
    //?                 returns Result<T> because external input is untrusted.
    //
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record TenantId
{
    #region Constants

    public const string Prefix = "tenant-";

    #endregion

    #region Properties

    //? The full identifier string, e.g. "tenant-019750ab-...".
    public string Value { get; }

    #endregion

    #region Constructor

    private TenantId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    //? Generates a fresh, guaranteed-valid TenantId. Format: "tenant-{Guid.CreateVersion7()}".
    public static TenantId New() => new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    /*
        //?     Reconstitutes a TenantId from an externally supplied string
        //?      (e.g., the "tenant_id" JWT claim or a route parameter).
        //
        //!     Returns a validation error when value is null, whitespace, or has the wrong prefix.
    */
    public static Result<TenantId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return TenantErrors.Id.InvalidFormat;
        }

        return new TenantId(value);
    }

    #endregion

    #region Overrides

    //? Returns the raw identifier string — makes logging and serialisation transparent.
    public override string ToString() => Value;

    #endregion
}
