using System.ComponentModel.DataAnnotations;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? Validated secondary phone number — same rules as the primary PhoneNumber.
 *> The "must differ from primary" rule is enforced at the aggregate level in User.Create,
 *>  not here, because value objects do not reference each other.
 *! The constructor is [private] — use Create() to get a validated instance.
 */
public sealed record SecondaryPhoneNumber
{
    #region Constants, Properties and Constructor

    public const int MaxLength = 20;

    public string Value { get; }

    //// private static readonly Regex E164Pattern = new(@"^\+[1-9]\d{9,14}$", RegexOptions.Compiled);

    private SecondaryPhoneNumber(string value) => Value = value;

    #endregion Constants, Properties and Constructor

    #region Factory Method

    //? Creates a validated SecondaryPhoneNumber. Collects all broken rules before returning.
    public static Result<SecondaryPhoneNumber> Create(string value)
    {
        var errors = new List<Error>();

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.SecondaryPhoneNumber.TooLong);
        }

        //? Base Class Library (BCL) equivalent of MailAddress() for phone numbers — `PhoneAttribute` from System.ComponentModel.DataAnnotations.
        //! PhoneAttribute accepts broader formats than strict E.164 (e.g. "(212) 555-1234" also passes).
        //* Swap for a Regex if strict E.164 enforcement is required: ^\+[1-9]\d{9,14}$
        if (!new PhoneAttribute().IsValid(trimmed))
        {
            errors.Add(UserErrors.SecondaryPhoneNumber.InvalidFormat);
        }

        //// if (!E164Pattern.IsMatch(trimmed))
        //// {
        ////     errors.Add(UserErrors.SecondaryPhoneNumber.InvalidFormat);
        //// }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new SecondaryPhoneNumber(trimmed);
    }

    #endregion Factory Method
}
