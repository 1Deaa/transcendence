using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

/*
//? Strongly-typed, lexicographically-sortable identity for an EmailTemplate.
//?
//? Format: "email_template-{UUIDv7}"
//?   - The prefix makes IDs self-describing in logs across service boundaries.
//?   - UUIDv7 embeds a millisecond timestamp — natural creation-order sort.
//
//! New() for fresh templates; From() when reconstituting from external input (API/DB).
*/
public sealed record EmailTemplateId
{
    private const string Prefix = "email_template-";

    public string Value { get; }

    private EmailTemplateId(string value) => Value = value;

    public static EmailTemplateId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<EmailTemplateId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmailTemplateErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return EmailTemplateErrors.Id.InvalidFormat;
        }

        return new EmailTemplateId(value);
    }

    public override string ToString() => Value;
}
