using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

/*
//? Base for entities that are soft-deletable but do not require an audit trail.
//? Rare in practice — prefer ASoftDeletableAuditableEntity<TId> for domain aggregates.
//
//! IsDeleted has a private setter. Only MarkAsDeleted() — declared here — can flip it.
//! EF Core materializes private setters fine via compiled expression trees.
*/
public abstract class ASoftDeletableEntity<TId> : AEntity<TId>, ISoftDeletable
{
    protected ASoftDeletableEntity(TId id)
        : base(id) { }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected ASoftDeletableEntity()
        : base() { }

    public bool IsDeleted { get; private set; }

    public void MarkAsDeleted() => IsDeleted = true;
}
