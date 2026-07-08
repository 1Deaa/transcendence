using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Domain.Entities.LeaveRequests.Events;

public sealed record LeaveRequestDecidedDomainEvent(
    LeaveRequestId LeaveRequestId,
    EmployeeId EmployeeId,
    LeaveStatus Decision
) : IDomainEvent;
