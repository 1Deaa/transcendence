using System.Text.RegularExpressions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Tenants.ValueObjects;

/*
    //?     The URL-safe workspace identifier used in tenant addressing (e.g. "acme" → acme.hrmsystem.com).
    //?     Lowercase letters/digits with single hyphen separators, 2–MaxLength characters.
    //!     Immutable after creation — a slug is part of every URL, bookmark, and email the tenant receives.
*/
public sealed partial record TenantSlug
{
    #region Constants

    public const int MaxLength = 50;
    public const int MinLength = 2;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private TenantSlug(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated TenantSlug — collects all errors before returning. Input is trimmed + lowercased.
    public static Result<TenantSlug> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(TenantErrors.Slug.Required);
        }
        else
        {
            if (value.Length is < MinLength or > MaxLength)
            {
                errors.Add(TenantErrors.Slug.InvalidLength);
            }

            if (!SlugRegex().IsMatch(value))
            {
                errors.Add(TenantErrors.Slug.InvalidFormat);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new TenantSlug(value!);
    }

    #endregion

    //? Source-generated for zero-allocation matching on the hot path.
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();
}
