using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using HrmSystem.Infrastructure.Persistence.Configurations.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Employees;

internal sealed class EmployeeConfiguration : ATenantEntityConfiguration<Employee, EmployeeId>
{
    public override void Configure(EntityTypeBuilder<Employee> employeeBuilder)
    {
        base.Configure(employeeBuilder); //! auditing + soft delete + IsActive + TenantId

        employeeBuilder.HasKey(e => e.Id);

        employeeBuilder.ToTable(
            DbConfigurationSettings.Tables.Employees,
            DbConfigurationSettings.Schemas.Application
        );

        employeeBuilder
            .Property(e => e.Id)
            .HasConversion<EmployeeIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        employeeBuilder
            .Property(e => e.DepartmentId)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("DepartmentId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        employeeBuilder
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        employeeBuilder.Property(e => e.HiredOn).IsRequired();
        employeeBuilder.Property(e => e.TerminatedOn).IsRequired(false);

        #region Owned Value Objects

        employeeBuilder.OwnsOne(
            e => e.Name,
            nameBuilder =>
            {
                nameBuilder
                    .Property(n => n.FirstName)
                    .HasColumnName("FirstName")
                    .HasMaxLength(PersonName.PartMaxLength)
                    .IsRequired();

                nameBuilder
                    .Property(n => n.LastName)
                    .HasColumnName("LastName")
                    .HasMaxLength(PersonName.PartMaxLength)
                    .IsRequired();
            }
        );

        employeeBuilder.OwnsOne(
            e => e.JobTitle,
            jobTitleBuilder =>
                jobTitleBuilder
                    .Property(j => j.Value)
                    .HasColumnName("JobTitle")
                    .HasMaxLength(JobTitle.MaxLength)
                    .IsRequired()
        );

        #endregion Owned Value Objects

        //! Converter-mapped (not OwnsOne) so the (TenantId, Email) composite index below is legal.
        employeeBuilder
            .Property(e => e.Email)
            .HasConversion<EmployeeEmailConverter>()
            .HasColumnName("Email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        #region Relationship

        //! 1 Department : M Employees. NoAction — removing a department must not cascade-delete staff.
        employeeBuilder
            .HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region Optimistic Concurrency

        employeeBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        #region Indexes

        //! Work email is unique per tenant.
        employeeBuilder
            .HasIndex(e => new { e.TenantId, e.Email })
            .IsUnique()
            .HasDatabaseName("IX_Employees_TenantId_Email");

        employeeBuilder
            .HasIndex(e => new { e.TenantId, e.DepartmentId })
            .HasDatabaseName("IX_Employees_TenantId_DepartmentId");

        employeeBuilder
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Employees_TenantId_Status");

        #endregion
    }
}
