using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Departments.AddDepartment;

public sealed record AddDepartmentCommand(string Name, string Code) : ICommand<string>;
