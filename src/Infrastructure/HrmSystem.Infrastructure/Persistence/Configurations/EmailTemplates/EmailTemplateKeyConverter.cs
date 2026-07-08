using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.EmailTemplates;

/*
//?  Converts EmailTemplateKey (smart enum) to/from its string Value for DB storage.
//?  Write: EmailTemplateKey → key.Value  (e.g. "Habit.Created")
//?  Read:  string → EmailTemplateKey.From(rawStr), throwing if the DB holds an unknown key.
//
//!  An InvalidOperationException on read means a key was removed from EmailTemplateKey
//!  without a migration to clean up the old rows — fix the data, not the exception.
*/
internal sealed class EmailTemplateKeyConverter : ValueConverter<EmailTemplateKey, string>
{
    public EmailTemplateKeyConverter()
        : base(
            key => key.Value,
            rawStr => ConvertToEmailTemplateKey(rawStr)
        )
    { }

    private static EmailTemplateKey ConvertToEmailTemplateKey(string rawStr)
    {
        Result<EmailTemplateKey> result = EmailTemplateKey.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains an unregistered EmailTemplateKey: '{rawStr}'."
                    + $" Add it to EmailTemplateKey._knownKeys or clean up the stale row."
            );
        }

        return result.Value;
    }
}
