using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

namespace HrmSystem.Domain.Entities.EmailTemplates;

/*
//? Aggregate root for a named, editable email message contract.
//?
//? Each template is identified by a unique EmailTemplateKey (e.g., "Habit.Created").
//? Subject and body may contain {{Placeholder}} tokens substituted at send-time by
//? ITemplateRenderer in the Application layer.
//?
//> Rows are seeded via EF Core data migrations — Infrastructure owns initial content.
//> Application code always fetches by key, never by ID:
//>   IEmailTemplateRepository.FindByKeyAsync(EmailTemplateKey.HabitCreated, ct)
//
//! Private constructor + static factory: no invalid template can exist in memory.
//! Parameterless private constructor exists solely for EF Core materialization.
//! Soft-delete is inherited from ASoftDeletableAuditableEntity<TId> with a private setter.
*/
public class EmailTemplate : ASoftDeletableAuditableEntity<EmailTemplateId>
{
    #region Constructor

    private EmailTemplate(
        EmailTemplateId id,
        EmailTemplateKey key,
        EmailTemplateSubject subject,
        EmailTemplateBody body
    )
        : base(id)
    {
        Key = key;
        Subject = subject;
        Body = body;
    }

    //! For EF Core materialisation only — never use in domain or application code.
    private EmailTemplate()
        : base() { }

    #endregion

    #region Properties

    //? Unique domain identifier for this template (e.g., "Habit.Created").
    public EmailTemplateKey Key { get; private set; } = null!;

    //? The subject line. May contain {{Placeholder}} tokens.
    public EmailTemplateSubject Subject { get; private set; } = null!;

    //? The message body. May contain {{Placeholder}} tokens.
    public EmailTemplateBody Body { get; private set; } = null!;

    #endregion

    #region Static factory

    /*
    //? The only way to produce a valid EmailTemplate.
    //? All validation errors are collected before returning.
    */
    public static Result<EmailTemplate> Create(string key, string subject, string body)
    {
        var errors = new List<Error>();

        Result<EmailTemplateKey> keyResult = EmailTemplateKey.From(key);
        if (keyResult.IsFailure)
        {
            errors.AddRange(keyResult.Errors);
        }

        Result<EmailTemplateSubject> subjectResult = EmailTemplateSubject.Create(subject);
        if (subjectResult.IsFailure)
        {
            errors.AddRange(subjectResult.Errors);
        }

        Result<EmailTemplateBody> bodyResult = EmailTemplateBody.Create(body);
        if (bodyResult.IsFailure)
        {
            errors.AddRange(bodyResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new EmailTemplate(
            EmailTemplateId.New(),
            keyResult.Value,
            subjectResult.Value,
            bodyResult.Value
        );
    }

    #endregion

    #region Domain methods

    //? Replaces the subject. Use when an admin edits a template via the admin panel.
    public Result UpdateSubject(string subject)
    {
        Result<EmailTemplateSubject> subjectResult = EmailTemplateSubject.Create(subject);
        if (subjectResult.IsFailure)
        {
            return subjectResult.Errors.ToList();
        }

        Subject = subjectResult.Value;
        return Result.Success();
    }

    //? Replaces the body. Use when an admin edits a template via the admin panel.
    public Result UpdateBody(string body)
    {
        Result<EmailTemplateBody> bodyResult = EmailTemplateBody.Create(body);
        if (bodyResult.IsFailure)
        {
            return bodyResult.Errors.ToList();
        }

        Body = bodyResult.Value;
        return Result.Success();
    }

    #endregion
}
