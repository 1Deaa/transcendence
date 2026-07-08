using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.ExportEmployees;

internal sealed class ExportEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<ExportEmployeesQuery, IReadOnlyList<EmployeeExportRow>>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    public async Task<Result<IReadOnlyList<EmployeeExportRow>>> Handle(
        ExportEmployeesQuery query,
        CancellationToken cancellationToken
    )
    {
        //? Tenant query filter scopes the export — a tenant can only ever export its own staff.
        IEnumerable<Employee> employees = await _employeeRepository.GetAllAsync(
            cancellationToken
        );

        IReadOnlyList<EmployeeExportRow> rows = employees
            .Select(EmployeeExportRow.FromEmployee)
            .ToList();

        return Result.Success(rows);
    }
}
