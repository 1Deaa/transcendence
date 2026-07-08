using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.EmailTemplates;

internal sealed class EmailTemplateIdConverter : ValueConverter<EmailTemplateId, string>
{
    public EmailTemplateIdConverter()
        : base(
            id => id.Value,
            rawStr => ConvertToEmailTemplateId(rawStr)
        )
    { }

    private static EmailTemplateId ConvertToEmailTemplateId(string rawStr)
    {
        Result<EmailTemplateId> result = EmailTemplateId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted EmailTemplateId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
