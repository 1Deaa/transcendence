using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.ValueObjects;
using HrmSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Operations;

internal sealed class HealthCheckSnapshotIdConverter
    : ValueConverter<HealthCheckSnapshotId, string>
{
    public HealthCheckSnapshotIdConverter()
        : base(id => id.Value, rawStr => ConvertToId(rawStr)) { }

    private static HealthCheckSnapshotId ConvertToId(string rawStr)
    {
        Result<HealthCheckSnapshotId> result = HealthCheckSnapshotId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted HealthCheckSnapshotId: '{rawStr}'."
            );
        }

        return result.Value;
    }
}

/*
    //!     Host-level, append-only probe log — plain IEntityTypeConfiguration on purpose:
    //!     no audit columns (CheckedAtUtc IS the timestamp), no soft delete, no tenant filter.
*/
internal sealed class HealthCheckSnapshotConfiguration
    : IEntityTypeConfiguration<HealthCheckSnapshot>
{
    public void Configure(EntityTypeBuilder<HealthCheckSnapshot> snapshotBuilder)
    {
        snapshotBuilder.HasKey(s => s.Id);

        snapshotBuilder.ToTable(
            "HealthCheckSnapshots",
            DbConfigurationSettings.Schemas.Application
        );

        snapshotBuilder
            .Property(s => s.Id)
            .HasConversion<HealthCheckSnapshotIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        snapshotBuilder.Property(s => s.ComponentName).HasMaxLength(100).IsRequired();

        snapshotBuilder
            .Property(s => s.State)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        snapshotBuilder.Property(s => s.DurationMs).IsRequired();
        snapshotBuilder.Property(s => s.Description).HasMaxLength(1000).IsRequired(false);
        snapshotBuilder.Property(s => s.CheckedAtUtc).IsRequired();

        //* Serves per-component uptime aggregates and the history chart's time-window scans.
        snapshotBuilder
            .HasIndex(s => new { s.ComponentName, s.CheckedAtUtc })
            .HasDatabaseName("IX_HealthCheckSnapshots_ComponentName_CheckedAtUtc");

        //* Serves the retention purge (delete WHERE CheckedAtUtc < cutoff).
        snapshotBuilder
            .HasIndex(s => s.CheckedAtUtc)
            .HasDatabaseName("IX_HealthCheckSnapshots_CheckedAtUtc");
    }
}
