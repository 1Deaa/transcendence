using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Employees.ExportEmployees;

public sealed record ExportEmployeesQuery : IQuery<IReadOnlyList<EmployeeExportRow>>;
