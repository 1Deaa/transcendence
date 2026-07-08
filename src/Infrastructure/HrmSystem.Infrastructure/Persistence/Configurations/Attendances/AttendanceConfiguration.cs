using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using HrmSystem.Infrastructure.Persistence.Configurations.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Attendances;

internal sealed class AttendanceConfiguration
    : ATenantEntityConfiguration<Attendance, AttendanceId>
{
    public override void Configure(EntityTypeBuilder<Attendance> attendanceBuilder)
    {
        base.Configure(attendanceBuilder); //! auditing + soft delete + IsActive + TenantId

        attendanceBuilder.HasKey(a => a.Id);

        attendanceBuilder.ToTable(
            DbConfigurationSettings.Tables.Attendances,
            DbConfigurationSettings.Schemas.Application
        );

        attendanceBuilder
            .Property(a => a.Id)
            .HasConversion<AttendanceIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        attendanceBuilder
            .Property(a => a.EmployeeId)
            .HasConversion<EmployeeIdConverter>()
            .HasColumnName("EmployeeId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        attendanceBuilder
            .Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        attendanceBuilder.Property(a => a.Date).IsRequired();
        attendanceBuilder.Property(a => a.ClockInAt).IsRequired(false);
        attendanceBuilder.Property(a => a.ClockOutAt).IsRequired(false);
        attendanceBuilder.Property(a => a.WorkedMinutes).IsRequired(false);

        #region Relationship

        //! 1 Employee : M Attendances. NoAction — attendance history outlives employment changes.
        attendanceBuilder
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region Optimistic Concurrency

        attendanceBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        #region Indexes

        /*
            //!     THE core business invariant: one attendance row per employee per working day.
            //!     The ClockIn handler translates a violation of this index into
            //!     AttendanceErrors.AlreadyClockedIn (409 Conflict).
        */
        attendanceBuilder
            .HasIndex(a => new
            {
                a.TenantId,
                a.EmployeeId,
                a.Date,
            })
            .IsUnique()
            .HasDatabaseName("IX_Attendances_TenantId_EmployeeId_Date");

        //* Serves the daily-attendance dashboard query (all employees, one day).
        attendanceBuilder
            .HasIndex(a => new { a.TenantId, a.Date })
            .HasDatabaseName("IX_Attendances_TenantId_Date");

        #endregion
    }
}
