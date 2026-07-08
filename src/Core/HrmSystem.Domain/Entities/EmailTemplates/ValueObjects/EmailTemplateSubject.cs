using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

/*
//? Trimmed, non-empty email subject line. Capped at MaxLength to stay within RFC 5321 guidance.
//! Constructor is private — use Create().
*/
public sealed record EmailTemplateSubject
{
    public const int MaxLength = 300;

    public string Value { get; }

    private EmailTemplateSubject(string value) => Value = value;

    public static Result<EmailTemplateSubject> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(EmailTemplateErrors.Subject.Required);
            return errors;
        }

        value = value.Trim();

        if (value.Length > MaxLength)
        {
            errors.Add(EmailTemplateErrors.Subject.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new EmailTemplateSubject(value);
    }

    public override string ToString() => Value;
}
