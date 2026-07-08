using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Employees;

internal sealed class EmployeeIdConverter : ValueConverter<EmployeeId, string>
{
    public EmployeeIdConverter()
        : base(employeeId => employeeId.Value, rawStr => ConvertToEmployeeId(rawStr)) { }

    private static EmployeeId ConvertToEmployeeId(string rawStr)
    {
        Result<EmployeeId> result = EmployeeId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted EmployeeId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
