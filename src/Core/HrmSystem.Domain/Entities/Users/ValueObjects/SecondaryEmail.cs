using System.Net.Mail;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? Validated secondary email address — same format rules as the primary Email.
 *> The "must differ from primary" rule is enforced at the aggregate level in User.Create,
 *>  not here, because value objects do not reference each other.
 *! The constructor is [private] — use Create() to get a validated instance.
 */
public sealed record SecondaryEmail
{
    #region Constants, Properties and Constructor

    public const int MaxLength = 254;

    public string Value { get; }

    private SecondaryEmail(string value) => Value = value;

    #endregion Constants, Properties and Constructor

    #region Factory Method

    //? Creates a validated SecondaryEmail. Normalises to lowercase. Collects all broken rules before returning.
    public static Result<SecondaryEmail> Create(string value)
    {
        var errors = new List<Error>();

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.SecondaryEmail.TooLong);
        }

        //? We Got some benefits of using the Framework by using: `MailAddress()`
        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (Exception ex)
        {
            errors.Add(UserErrors.SecondaryEmail.InvalidFormat);
            errors.Add(Error.Validation("SecondaryEmail.ParseDetail", ex.Message));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new SecondaryEmail(trimmed.ToLowerInvariant());
    }

    #endregion Factory Method
}
