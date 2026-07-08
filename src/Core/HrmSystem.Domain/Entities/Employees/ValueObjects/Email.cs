using System.Text.RegularExpressions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Employees.ValueObjects;

/*
    //?     A validated, lowercased work email address (max MaxLength characters).
    //!     The regex is a pragmatic RFC-lite check — deliverability is verified by the
    //!     email-confirmation flow, not by the domain model.
*/
public sealed partial record Email
{
    #region Constants

    public const int MaxLength = 320;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private Email(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated Email — trims + lowercases, collects all errors before returning.
    public static Result<Email> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(EmployeeErrors.Email.Required);
        }
        else
        {
            if (value.Length > MaxLength)
            {
                errors.Add(EmployeeErrors.Email.TooLong);
            }

            if (!EmailRegex().IsMatch(value))
            {
                errors.Add(EmployeeErrors.Email.InvalidFormat);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new Email(value!);
    }

    #endregion

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
