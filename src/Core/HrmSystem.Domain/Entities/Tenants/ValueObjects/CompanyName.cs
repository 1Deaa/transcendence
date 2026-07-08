using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Tenants.ValueObjects;

/*
    //?     A non-empty, trimmed company display name (max MaxLength characters).
    //!     The constructor is [private] — use Create() to get a validated instance.
*/
public sealed record CompanyName
{
    #region Constants

    public const int MaxLength = 200;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private CompanyName(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated CompanyName — collects all errors before returning.
    public static Result<CompanyName> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(TenantErrors.CompanyName.Required);
        }
        else if (value.Length > MaxLength)
        {
            errors.Add(TenantErrors.CompanyName.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new CompanyName(value!);
    }

    #endregion
}
