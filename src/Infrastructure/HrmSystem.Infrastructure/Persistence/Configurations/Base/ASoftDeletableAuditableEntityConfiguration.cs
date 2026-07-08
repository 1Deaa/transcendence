using HrmSystem.Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Base;

internal abstract class ASoftDeletableAuditableEntityConfiguration<TEntity, TId>
    : AAuditableEntityConfiguration<TEntity, TId>
    where TEntity : ASoftDeletableAuditableEntity<TId>
    where TId : class
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder); //! For auditing fields from Level 1

        //*    HasDefaultValue(false) → DEFAULT ((0)). (As it is Bits).
        //*    IsRequired() → NOT NULL in SQL.
        builder.Property(s => s.IsDeleted).HasDefaultValue(false).IsRequired();

        /*
            //?  Global query filter — EF Core appends this as a WHERE clause to EVERY query on this entity.
            //?  GetByIdAsync, GetAllAsync, FindUsingPredicate — all automatically exclude soft-deleted rows.
            //>  Generated SQL example: SELECT * FROM Habits WHERE Id = @id AND IsDeleted = 0
            //
            //!  To intentionally bypass this filter (admin restore, background job), use FindByIdAsync
            //!  which calls .IgnoreQueryFilters() explicitly.
        */
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
