using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

/*
//? Base for the most common aggregate pattern: auditable + soft-deletable.
//? Use for entities like Habit and EmailTemplate that are logically deleted, never physically removed.
//
//! IsDeleted has a private setter. Only MarkAsDeleted() — declared here — can flip it.
//! EF Core materializes private setters fine via compiled expression trees.
*/
public abstract class ASoftDeletableAuditableEntity<TId> : AAuditableEntity<TId>, ISoftDeletable
{
    protected ASoftDeletableAuditableEntity(TId id)
        : base(id) { }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected ASoftDeletableAuditableEntity()
        : base() { }

    public bool IsDeleted { get; private set; }

    public void MarkAsDeleted() => IsDeleted = true;
}
