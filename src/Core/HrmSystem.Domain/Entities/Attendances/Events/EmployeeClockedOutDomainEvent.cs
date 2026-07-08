using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Domain.Entities.Attendances.Events;

public sealed record EmployeeClockedOutDomainEvent(AttendanceId AttendanceId, EmployeeId EmployeeId)
    : IDomainEvent;
