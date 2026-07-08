using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Employees.TerminateEmployee;

public sealed record TerminateEmployeeCommand(string EmployeeId, DateOnly TerminatedOn) : ICommand;
