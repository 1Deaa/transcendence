using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;

namespace HrmSystem.Application.Features.LeaveRequests.SubmitLeaveRequest;

public sealed record SubmitLeaveRequestCommand(
    string EmployeeId,
    LeaveType Type,
    DateOnly Start,
    DateOnly End,
    string Reason
) : ICommand<string>;
