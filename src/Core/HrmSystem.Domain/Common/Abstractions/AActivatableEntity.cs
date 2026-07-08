using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

/*
//? Base for entities that are activatable/deactivatable but do not require an audit trail.
//? Rare in practice — prefer AActivatableAuditableEntity<TId> for domain aggregates.
//
//! IsActive has a private setter. Only Activate() and Deactivate() — declared here — can change it.
//! Entities start active (IsActive = true) via the parameterized constructor.
//! EF Core materializes private setters fine via compiled expression trees.
*/
public abstract class AActivatableEntity<TId> : AEntity<TId>, IActivatable
{
    protected AActivatableEntity(TId id)
        : base(id)
    {
        IsActive = true;
    }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected AActivatableEntity()
        : base() { }

    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
