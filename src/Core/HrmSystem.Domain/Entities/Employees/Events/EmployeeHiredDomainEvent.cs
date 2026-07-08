using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Domain.Entities.Employees.Events;

public sealed record EmployeeHiredDomainEvent(EmployeeId EmployeeId) : IDomainEvent;
