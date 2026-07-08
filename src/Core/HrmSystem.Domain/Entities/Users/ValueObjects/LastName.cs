using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

//? Validated last name — trimmed, non-empty, max 100 characters.
public sealed record LastName
{
    #region Consts & Properties & Constructor

    public const int MaxLength = 100;

    public string Value { get; }

    private LastName(string value) => Value = value;

    #endregion

    #region Factory & Methods

    //? Creates a validated LastName. Collects all broken rules before returning.
    public static Result<LastName> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(UserErrors.LastName.Required);
            return errors;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.LastName.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new LastName(trimmed);
    }

    #endregion
}
