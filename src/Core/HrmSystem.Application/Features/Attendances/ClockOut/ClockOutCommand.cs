using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Attendances.ClockOut;

public sealed record ClockOutCommand(string EmployeeId) : ICommand;
