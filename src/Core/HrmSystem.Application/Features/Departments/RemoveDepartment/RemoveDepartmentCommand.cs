using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Departments.RemoveDepartment;

public sealed record RemoveDepartmentCommand(string DepartmentId) : ICommand;
