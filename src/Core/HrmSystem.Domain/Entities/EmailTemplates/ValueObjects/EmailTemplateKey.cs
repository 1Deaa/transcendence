using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;

/*
//? Smart-enum for the known set of email template keys.
//?
//? Static instances give compile-time safety when referencing keys from application code.
//? From() allows DB hydration and validates the value is in the known set — preventing
//? ghost templates from being loaded and silently ignored at runtime.
//?
//> Convention: "Aggregate.Event" — mirrors the domain event name for discoverability.
//> To add a new template: declare a static instance here, add it to _knownKeys,
//>  then create the template row in the seed migration.
//
//! Never pass ad-hoc strings where an EmailTemplateKey is expected.
//!  Always use the static instances (EmailTemplateKey.HabitCreated) from application code.
*/
public sealed record EmailTemplateKey
{
    //> Habit lifecycle
    public static readonly EmailTemplateKey HabitCreated = new("Habit.Created");
    public static readonly EmailTemplateKey HabitCompleted = new("Habit.Completed");
    public static readonly EmailTemplateKey HabitArchived = new("Habit.Archived");

    //> User lifecycle
    public static readonly EmailTemplateKey WelcomeUser = new("User.Welcome");
    public static readonly EmailTemplateKey UserDeleted = new("User.Deleted");
    public static readonly EmailTemplateKey UserPasswordReset = new("User.PasswordReset");
    public static readonly EmailTemplateKey UserEmailConfirmation = new("User.EmailConfirmation");

    private static readonly HashSet<string> _knownKeys =
    [
        HabitCreated.Value,
        HabitCompleted.Value,
        HabitArchived.Value,
        WelcomeUser.Value,
        UserPasswordReset.Value,
        UserEmailConfirmation.Value,
    ];

    public string Value { get; }

    private EmailTemplateKey(string value) => Value = value;

    public static Result<EmailTemplateKey> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmailTemplateErrors.Key.Required;
        }

        if (!_knownKeys.Contains(value))
        {
            return EmailTemplateErrors.Key.Unknown;
        }

        return new EmailTemplateKey(value);
    }

    public override string ToString() => Value;
}
