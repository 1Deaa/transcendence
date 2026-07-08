using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.LeaveRequests.ApproveLeaveRequest;

public sealed record ApproveLeaveRequestCommand(string LeaveRequestId) : ICommand;
