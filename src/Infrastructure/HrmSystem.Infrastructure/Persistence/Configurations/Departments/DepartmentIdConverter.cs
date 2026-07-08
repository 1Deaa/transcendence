using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Departments;

internal sealed class DepartmentIdConverter : ValueConverter<DepartmentId, string>
{
    public DepartmentIdConverter()
        : base(departmentId => departmentId.Value, rawStr => ConvertToDepartmentId(rawStr)) { }

    private static DepartmentId ConvertToDepartmentId(string rawStr)
    {
        Result<DepartmentId> result = DepartmentId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted DepartmentId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
