using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Employees;

/*
    //?     Email is converter-mapped (NOT OwnsOne) so the per-tenant unique index
    //?     (TenantId, Email) can be declared as a plain composite index on the owner —
    //?     owner + owned-type columns cannot share one HasIndex call.
*/
internal sealed class EmployeeEmailConverter : ValueConverter<Email, string>
{
    public EmployeeEmailConverter()
        : base(email => email.Value, rawStr => ConvertToEmail(rawStr)) { }

    private static Email ConvertToEmail(string rawStr)
    {
        Result<Email> result = Email.Create(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted employee Email: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
