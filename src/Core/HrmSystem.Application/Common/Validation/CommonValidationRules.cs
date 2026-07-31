using System.Net.Mail;
using FluentValidation;

namespace HrmSystem.Application.Common.Validation;

/*
    //?     Shared FluentValidation rules so every command that accepts an email or a
    //?     password enforces exactly the same policy as ASP.NET Identity does later.
    //!     Keep in sync with the Identity password options in
    //!     Infrastructure/DependencyInjection.AddIdentityAuthenticationService.
*/
public static class CommonValidationRules
{
    public const int MinimumPasswordLength = 8;

    /*
        //!     FluentValidation's EmailAddress() only checks for a single '@' — it accepts
        //!     "test@test." (empty TLD). MailAddress.TryCreate plus a dotted-host check
        //!     rejects incomplete domains while staying permissive on real addresses.
    */
    public static IRuleBuilderOptions<T, string?> StrictEmailAddress<T>(
        this IRuleBuilder<T, string?> ruleBuilder
    ) =>
        ruleBuilder
            .Must(email =>
                email is not null
                && MailAddress.TryCreate(email, out MailAddress? parsed)
                && parsed.Address == email
                && parsed.Host.Contains('.')
                && !parsed.Host.StartsWith('.')
                && !parsed.Host.EndsWith('.')
            )
            .WithMessage("'{PropertyName}' must be a valid email address with a full domain (e.g. name@company.com).");

    //? Mirrors the Identity policy: min 8 chars with a digit, an uppercase and a lowercase letter.
    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(
        this IRuleBuilder<T, string?> ruleBuilder
    ) =>
        ruleBuilder
            .MinimumLength(MinimumPasswordLength)
            .Must(password => password is not null && password.Any(char.IsDigit))
            .WithMessage("'{PropertyName}' must contain at least one digit.")
            .Must(password => password is not null && password.Any(char.IsUpper))
            .WithMessage("'{PropertyName}' must contain at least one uppercase letter.")
            .Must(password => password is not null && password.Any(char.IsLower))
            .WithMessage("'{PropertyName}' must contain at least one lowercase letter.");
}
