using System.ComponentModel.DataAnnotations;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? Validated phone number — max 20 characters. Example: +12125551234.
 *! The constructor is [private] — use Create() to get a validated instance.
 */
public sealed record PhoneNumber
{
    #region Old Regex ValidPattern Code

    //private static readonly Regex ValidPattern = new(
    //    @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
    //    RegexOptions.Compiled | RegexOptions.IgnoreCase
    //);

    //if (!ValidPattern.IsMatch(trimmed))
    //{
    //    errors.Add(UserErrors.SecondaryEmail.InvalidFormat);
    //}

    #endregion Old Regex ValidPattern Code


    #region Constants, Properties and Constructor

    public const int MaxLength = 20;

    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    #endregion Constants, Properties and Constructor


    #region Factory Method

    //? Creates a validated PhoneNumber. Collects all broken rules before returning.
    public static Result<PhoneNumber> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(UserErrors.PhoneNumber.InvalidFormat);
            return errors;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.PhoneNumber.TooLong);
        }

        //? BCL equivalent of MailAddress() for phone numbers — `PhoneAttribute` from System.ComponentModel.DataAnnotations.
        //! PhoneAttribute accepts broader formats than strict E.164 (e.g. "(212) 555-1234" also passes).
        //* Swap for a Regex if strict E.164 enforcement is required: ^\+[1-9]\d{9,14}$
        if (!new PhoneAttribute().IsValid(trimmed))
        {
            errors.Add(UserErrors.PhoneNumber.InvalidFormat);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new PhoneNumber(trimmed);
    }

    #endregion Factory Method
}
