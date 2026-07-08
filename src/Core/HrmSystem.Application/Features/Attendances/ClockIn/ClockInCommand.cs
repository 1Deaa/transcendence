using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Attendances.ClockIn;

public sealed record ClockInCommand(string EmployeeId) : ICommand<string>;
