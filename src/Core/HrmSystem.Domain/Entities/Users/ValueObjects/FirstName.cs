using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

//? Validated first name — trimmed, non-empty, max 100 characters.
public sealed record FirstName
{
    #region Constants

    public const int MaxLength = 100;

    #endregion

    #region Properties & Constructor
    private FirstName(string value) => Value = value;

    public string Value { get; }

    #endregion

    #region Factor

    //? Creates a validated FirstName. Collects all broken rules before returning.
    public static Result<FirstName> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(UserErrors.FirstName.Required);
            return errors;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.FirstName.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new FirstName(trimmed);
    }

    #endregion
}
