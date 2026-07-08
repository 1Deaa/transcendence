using System.Net.Mail;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Users.ValueObjects;

/*
 *? Validated email address — trimmed, lowercased, max 254 characters (RFC 5321 limit),
 *?  must contain a local part, @, domain, and TLD.
 *! The constructor is [private] — use Create() to get a validated instance.
 */
public sealed record Email
{
    #region Regex ValidPattern Old Approach

    // ! Good ; but we can Benefit from Framework Libraries:
    //private static readonly Regex ValidPattern =
    //    new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    //if (!ValidPattern.IsMatch(trimmed))
    //{
    //    errors.Add(UserErrors.Email.InvalidFormat);
    //}

    #endregion Regex ValidPattern Old Approach


    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value) => Value = value;

    //? Creates a validated Email. Normalises to lowercase. Collects all broken rules before returning.
    public static Result<Email> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(UserErrors.Email.Required);
            return errors;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            errors.Add(UserErrors.Email.TooLong);
        }

        //? We Got some benefits of using the Framework by using: `MailAddress()`
        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (Exception ex)
        {
            errors.Add(UserErrors.Email.InvalidFormat);
            errors.Add(Error.Validation("Email.ParseDetail", ex.Message));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new Email(trimmed.ToLowerInvariant());
    }
}
