using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Base;

/*
    //?     Level 4 of the base-configuration hierarchy — for TENANT-OWNED aggregates.
    //?     Adds on top of AFullAuditableEntityConfiguration:
    //?      - the TenantId column (converted strongly-typed VO, required, indexed).
    //
    //!     The tenant QUERY FILTER is NOT configured here — query filters must capture the
    //!     live DbContext instance to be parameterized per request, so ApplicationDbContext
    //!     applies the named "SoftDelete" + "Tenant" filters in OnModelCreating instead.
    //!     (An IEntityTypeConfiguration has no access to ITenantContext.)
*/
internal abstract class ATenantEntityConfiguration<TEntity, TId>
    : AFullAuditableEntityConfiguration<TEntity, TId>
    where TEntity : ATenantEntity<TId>
    where TId : class
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder); //! auditing + IsDeleted + IsActive from Levels 1–3

        builder
            .Property(t => t.TenantId)
            .HasConversion<TenantIdConverter>()
            .HasColumnName("TenantId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        //* Every tenant-owned table is always queried tenant-first — lead the index with TenantId.
        builder.HasIndex(t => t.TenantId);
    }
}
