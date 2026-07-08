using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Domain.Common.Abstractions;

/*
    //?     Base for every tenant-owned aggregate (Department, Employee, Attendance, LeaveRequest, ImportJob…).
    //?     Extends AFullAuditableEntity, so tenant-owned aggregates are automatically
    //?     auditable + soft-deletable + activatable as well.
    //
    //!     TenantId has a [private] setter and NO public mutator on purpose:
    //!      - Domain factories do not take a TenantId — handlers stay tenant-agnostic.
    //!      - The Infrastructure TenantWriteGuardInterceptor stamps it on Added entities
    //!        (via the EF change-tracker entry, which can write private setters) and
    //!        rejects any save whose TenantId mismatches the ambient ITenantContext.
    //
    //*     This keeps cross-tenant writes impossible by construction instead of by discipline.
    //
    //>     Reads are protected separately by the named EF Core tenant query filter
    //>     configured in ATenantEntityConfiguration.
*/
public abstract class ATenantEntity<TId> : AFullAuditableEntity<TId>, ITenantOwned
{
    protected ATenantEntity(TId id)
        : base(id) { }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected ATenantEntity()
        : base() { }

    //! Materialized by EF; stamped by TenantWriteGuardInterceptor before the first save.
    public TenantId TenantId { get; private set; } = null!;
}
