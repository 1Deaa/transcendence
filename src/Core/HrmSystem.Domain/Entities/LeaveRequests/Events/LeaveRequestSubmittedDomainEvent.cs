using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Domain.Entities.LeaveRequests.Events;

public sealed record LeaveRequestSubmittedDomainEvent(
    LeaveRequestId LeaveRequestId,
    EmployeeId EmployeeId
) : IDomainEvent;
