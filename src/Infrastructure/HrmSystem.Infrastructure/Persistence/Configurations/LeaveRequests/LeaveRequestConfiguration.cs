using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using HrmSystem.Infrastructure.Persistence.Configurations.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.LeaveRequests;

internal sealed class LeaveRequestConfiguration
    : ATenantEntityConfiguration<LeaveRequest, LeaveRequestId>
{
    public override void Configure(EntityTypeBuilder<LeaveRequest> leaveRequestBuilder)
    {
        base.Configure(leaveRequestBuilder); //! auditing + soft delete + IsActive + TenantId

        leaveRequestBuilder.HasKey(l => l.Id);

        leaveRequestBuilder.ToTable(
            DbConfigurationSettings.Tables.LeaveRequests,
            DbConfigurationSettings.Schemas.Application
        );

        leaveRequestBuilder
            .Property(l => l.Id)
            .HasConversion<LeaveRequestIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        leaveRequestBuilder
            .Property(l => l.EmployeeId)
            .HasConversion<EmployeeIdConverter>()
            .HasColumnName("EmployeeId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        leaveRequestBuilder
            .Property(l => l.Type)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        leaveRequestBuilder
            .Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        leaveRequestBuilder.Property(l => l.DecidedAt).IsRequired(false);

        //? DecidedBy stores the deciding user's domain UserId string ("user-<uuid>").
        leaveRequestBuilder
            .Property(l => l.DecidedBy)
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired(false);

        #region Owned Value Objects

        leaveRequestBuilder.OwnsOne(
            l => l.Period,
            periodBuilder =>
            {
                periodBuilder.Property(p => p.Start).HasColumnName("PeriodStart").IsRequired();
                periodBuilder.Property(p => p.End).HasColumnName("PeriodEnd").IsRequired();
            }
        );

        leaveRequestBuilder.OwnsOne(
            l => l.Reason,
            reasonBuilder =>
                reasonBuilder
                    .Property(r => r.Value)
                    .HasColumnName("Reason")
                    .HasMaxLength(LeaveReason.MaxLength)
                    .IsRequired()
        );

        #endregion Owned Value Objects

        #region Relationship

        //! 1 Employee : M LeaveRequests. NoAction — leave history outlives employment changes.
        leaveRequestBuilder
            .HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region Optimistic Concurrency

        leaveRequestBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        #region Indexes

        //* Serves "pending approvals" dashboards and per-employee leave history.
        leaveRequestBuilder
            .HasIndex(l => new { l.TenantId, l.Status })
            .HasDatabaseName("IX_LeaveRequests_TenantId_Status");

        leaveRequestBuilder
            .HasIndex(l => new { l.TenantId, l.EmployeeId })
            .HasDatabaseName("IX_LeaveRequests_TenantId_EmployeeId");

        #endregion
    }
}
