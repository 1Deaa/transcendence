using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

/*
//? Base for aggregates that are auditable + activatable but NOT soft-deletable.
//? Use for entities like subscription plans or feature-flag entities that can be
//? suspended/resumed without ever being logically deleted.
//
//! IsActive has a private setter. Only Activate() and Deactivate() — declared here — can change it.
//! Entities start active (IsActive = true) via the parameterized constructor.
//! EF Core materializes private setters fine via compiled expression trees.
*/
public abstract class AActivatableAuditableEntity<TId> : AAuditableEntity<TId>, IActivatable
{
    protected AActivatableAuditableEntity(TId id)
        : base(id)
    {
        IsActive = true;
    }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected AActivatableAuditableEntity()
        : base() { }

    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
