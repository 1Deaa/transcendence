using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.EmailTemplates;

public static class EmailTemplateErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "EmailTemplate.NotFound",
        "No email template was found for the requested key."
    );

    // ── EmailTemplateId value object ─────────────────────────────────────────

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "EmailTemplate.Id.Invalid",
            "The email template identifier is required and cannot be empty."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "EmailTemplate.Id.InvalidFormat",
            "The email template identifier format is invalid. Expected format: \"emailtemplate-{id}\"."
        );
    }

    // ── EmailTemplateKey value object ────────────────────────────────────────

    public static class Key
    {
        public static readonly Error Required = Error.Validation(
            "EmailTemplate.Key.Required",
            "The email template key is required and cannot be empty."
        );

        public static readonly Error Unknown = Error.Validation(
            "EmailTemplate.Key.Unknown",
            "The email template key is not registered. Add it to EmailTemplateKey before seeding."
        );
    }

    // ── EmailTemplateSubject value object ────────────────────────────────────

    public static class Subject
    {
        public static readonly Error Required = Error.Validation(
            "EmailTemplate.Subject.Required",
            "Email template subject is required and cannot be empty."
        );

        public static readonly Error TooLong = Error.Validation(
            "EmailTemplate.Subject.TooLong",
            $"Email template subject cannot exceed {ValueObjects.EmailTemplateSubject.MaxLength} characters."
        );
    }

    // ── EmailTemplateBody value object ───────────────────────────────────────

    public static class Body
    {
        public static readonly Error Required = Error.Validation(
            "EmailTemplate.Body.Required",
            "Email template body is required and cannot be empty."
        );

        public static readonly Error TooLong = Error.Validation(
            "EmailTemplate.Body.TooLong",
            $"Email template body cannot exceed {ValueObjects.EmailTemplateBody.MaxLength} characters."
        );
    }
}
