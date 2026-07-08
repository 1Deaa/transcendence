using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

/*
//? Trimmed, non-empty email body. Supports {{Placeholder}} substitution tokens.
//? MaxLength is generous enough for full rich-text email content.
//
//! Constructor is private — use Create().
//! The domain does not validate placeholder syntax — ITemplateRenderer owns that contract.
*/
public sealed record EmailTemplateBody
{
    public const int MaxLength = 8_000;

    public string Value { get; }

    private EmailTemplateBody(string value) => Value = value;

    public static Result<EmailTemplateBody> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(EmailTemplateErrors.Body.Required);
            return errors;
        }

        value = value.Trim();

        if (value.Length > MaxLength)
        {
            errors.Add(EmailTemplateErrors.Body.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new EmailTemplateBody(value);
    }

    public override string ToString() => Value;
}
