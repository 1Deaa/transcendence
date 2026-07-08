using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

public abstract class AAuditableEntity<TIdType> : AEntity<TIdType>, IAuditable
{
    protected AAuditableEntity(TIdType id)
        : base(id) { }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected AAuditableEntity()
        : base() { }

    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public void UpdateCreation(DateTime createdAt, string? createdBy)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public void UpdateModification(DateTime lastModifiedAt, string? lastModifiedBy)
    {
        LastModifiedAt = lastModifiedAt;
        LastModifiedBy = lastModifiedBy;
    }
}
