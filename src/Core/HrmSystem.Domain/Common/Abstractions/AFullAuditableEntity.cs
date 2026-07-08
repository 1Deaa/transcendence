using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

/*
//? Base for aggregates that are auditable, soft-deletable, AND activatable.
//? The canonical example is User: an account can be suspended (deactivated) or fully removed (soft-deleted).
//?
//> An entity can be Deactivated (suspended, still visible in admin queries) independently
//>  of being soft-deleted (removed from all normal query results via EF Core global filter).
//
//! Both IsDeleted and IsActive have private setters. State only changes through
//!  MarkAsDeleted(), Activate(), and Deactivate() — all declared here.
//! Entities start active (IsActive = true) via the parameterized constructor.
//! EF Core materializes private setters fine via compiled expression trees.
*/
public abstract class AFullAuditableEntity<TId> : ASoftDeletableAuditableEntity<TId>, IActivatable
{
    protected AFullAuditableEntity(TId id)
        : base(id)
    {
        IsActive = true;
    }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected AFullAuditableEntity()
        : base() { }

    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
