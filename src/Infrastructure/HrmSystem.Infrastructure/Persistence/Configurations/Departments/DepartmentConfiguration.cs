using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Departments;

internal sealed class DepartmentConfiguration
    : ATenantEntityConfiguration<Department, DepartmentId>
{
    public override void Configure(EntityTypeBuilder<Department> departmentBuilder)
    {
        base.Configure(departmentBuilder); //! auditing + soft delete + IsActive + TenantId

        departmentBuilder.HasKey(d => d.Id);

        departmentBuilder.ToTable(
            DbConfigurationSettings.Tables.Departments,
            DbConfigurationSettings.Schemas.Application
        );

        departmentBuilder
            .Property(d => d.Id)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        #region Owned Value Objects

        departmentBuilder.OwnsOne(
            d => d.Name,
            nameBuilder =>
                nameBuilder
                    .Property(n => n.Value)
                    .HasColumnName("Name")
                    .HasMaxLength(DepartmentName.MaxLength)
                    .IsRequired()
        );

        #endregion Owned Value Objects

        //! Converter-mapped (not OwnsOne) so the (TenantId, Code) composite index below is legal.
        departmentBuilder
            .Property(d => d.Code)
            .HasConversion<DepartmentCodeConverter>()
            .HasColumnName("Code")
            .HasMaxLength(DepartmentCode.MaxLength)
            .IsRequired();

        #region Optimistic Concurrency

        departmentBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        //! THE per-tenant business invariant: department codes are unique inside a workspace.
        departmentBuilder
            .HasIndex(d => new { d.TenantId, d.Code })
            .IsUnique()
            .HasDatabaseName("IX_Departments_TenantId_Code");
    }
}
