using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Domain.Entities.Tenants.Events;

public sealed record TenantCreatedDomainEvent(TenantId TenantId) : IDomainEvent;
