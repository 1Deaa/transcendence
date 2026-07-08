using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.LeaveRequests.RejectLeaveRequest;

public sealed record RejectLeaveRequestCommand(string LeaveRequestId) : ICommand;
