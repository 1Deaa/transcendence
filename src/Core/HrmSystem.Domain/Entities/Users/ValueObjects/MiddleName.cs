using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? Optional middle name — trimmed, max 100 characters.
 *> The aggregate keeps null when the user has no middle name; this value object is only
 *>  created when a value is actually provided.
 *! The constructor is [private] — use Create() to get a validated instance.
 */
public sealed record MiddleName
{
    #region Properties, Constants, Constructor

    public const int MaxLength = 100;

    public string Value { get; }

    private MiddleName(string value) => Value = value;

    #endregion


    #region Factory & Methods

    //? Creates a validated MiddleName from a non-empty string.
    //! Only call this when the caller has confirmed the value is present.
    public static Result<MiddleName> Create(string? value)
    {
        var errors = new List<Error>();

        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.MiddleName.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new MiddleName(trimmed);
    }

    #endregion
}
