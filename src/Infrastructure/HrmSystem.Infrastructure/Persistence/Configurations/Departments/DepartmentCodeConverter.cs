using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Departments;

/*
    //?     Code is converter-mapped (NOT OwnsOne) so the per-tenant unique index
    //?     (TenantId, Code) can be declared as a plain composite index on the owner —
    //?     owner + owned-type columns cannot share one HasIndex call.
*/
internal sealed class DepartmentCodeConverter : ValueConverter<DepartmentCode, string>
{
    public DepartmentCodeConverter()
        : base(code => code.Value, rawStr => ConvertToDepartmentCode(rawStr)) { }

    private static DepartmentCode ConvertToDepartmentCode(string rawStr)
    {
        Result<DepartmentCode> result = DepartmentCode.Create(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted DepartmentCode: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
