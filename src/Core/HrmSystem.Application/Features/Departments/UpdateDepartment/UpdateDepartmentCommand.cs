using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Departments.UpdateDepartment;

public sealed record UpdateDepartmentCommand(string DepartmentId, string Name, string Code)
    : ICommand;
