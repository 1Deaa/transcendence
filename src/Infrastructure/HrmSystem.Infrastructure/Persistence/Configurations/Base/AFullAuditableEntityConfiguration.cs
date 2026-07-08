using HrmSystem.Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Base;

internal abstract class AFullAuditableEntityConfiguration<TEntity, TId>
    : ASoftDeletableAuditableEntityConfiguration<TEntity, TId>
    where TEntity : AFullAuditableEntity<TId>
    where TId : class
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder); //! This configure the: auditing + IsDeleted from Level 2

        builder.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);

        /*
            //?  EF Core allows only ONE HasQueryFilter per entity type — the last call wins and REPLACES any
            //?  filter set by the base class. So this must repeat !IsDeleted AND add IsActive together.
            //
            //!  If we only wrote IsActive here, the !IsDeleted filter from Level 2 would be silently discarded.
            //!  Both conditions are required: a suspended (IsActive=false) user is still excluded from queries.
        */
        //builder.HasQueryFilter(e => !e.IsDeleted && e.IsActive);

        //! But; because I want to retrieve also to retrieve (Active, Not Active) records; But if I don't want I can do as above.
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
