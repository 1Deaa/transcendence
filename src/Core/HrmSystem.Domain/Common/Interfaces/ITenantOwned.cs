using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Domain.Common.Interfaces;

/*
    //?     Marks an aggregate as belonging to exactly one tenant (company workspace).
    //?     Infrastructure keys off this marker to:
    //?      - apply the named EF Core tenant query filter (reads),
    //?      - stamp/verify TenantId in the TenantWriteGuardInterceptor (writes).
    //
    //!     Getter-only on purpose — tenant ownership is assigned once (at creation, by the
    //!     write-guard interceptor) and never reassigned. No setter, no default implementation.
    //
    //>     Host-level entities (Tenant itself, BackupHistory, HealthCheckSnapshot, Identity users)
    //>     must NOT implement this interface — they are platform-wide, not tenant-scoped.
*/
public interface ITenantOwned
{
    TenantId TenantId { get; }
}
