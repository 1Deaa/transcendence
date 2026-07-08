using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Tenants;

//! Tenant is HOST-LEVEL — AFullAuditableEntityConfiguration (not ATenantEntityConfiguration).
internal sealed class TenantConfiguration : AFullAuditableEntityConfiguration<Tenant, TenantId>
{
    public override void Configure(EntityTypeBuilder<Tenant> tenantBuilder)
    {
        base.Configure(tenantBuilder);

        tenantBuilder.HasKey(t => t.Id);

        tenantBuilder.ToTable(
            DbConfigurationSettings.Tables.Tenants,
            DbConfigurationSettings.Schemas.Application
        );

        tenantBuilder
            .Property(t => t.Id)
            .HasConversion<TenantIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        tenantBuilder
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        tenantBuilder.Property(t => t.SuspendedAt).IsRequired(false);

        #region Owned Value Objects

        tenantBuilder.OwnsOne(
            t => t.CompanyName,
            companyNameBuilder =>
                companyNameBuilder
                    .Property(c => c.Value)
                    .HasColumnName("CompanyName")
                    .HasMaxLength(CompanyName.MaxLength)
                    .IsRequired()
        );

        tenantBuilder.OwnsOne(
            t => t.Slug,
            slugBuilder =>
            {
                slugBuilder
                    .Property(s => s.Value)
                    .HasColumnName("Slug")
                    .HasMaxLength(TenantSlug.MaxLength)
                    .IsRequired();

                //! Workspace slugs are globally unique — they live in URLs (acme.hrmsystem.com).
                slugBuilder.HasIndex(s => s.Value).IsUnique().HasDatabaseName("IX_Tenants_Slug");
            }
        );

        #endregion Owned Value Objects

        #region Optimistic Concurrency

        tenantBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion
    }
}
