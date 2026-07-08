using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Operations.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Operations;

internal sealed class BackupHistoryIdConverter : ValueConverter<BackupHistoryId, string>
{
    public BackupHistoryIdConverter()
        : base(id => id.Value, rawStr => ConvertToId(rawStr)) { }

    private static BackupHistoryId ConvertToId(string rawStr)
    {
        Result<BackupHistoryId> result = BackupHistoryId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted BackupHistoryId: '{rawStr}'."
            );
        }

        return result.Value;
    }
}

//! Host-level table — AAuditableEntityConfiguration (no soft delete, no tenant filter).
internal sealed class BackupHistoryConfiguration
    : AAuditableEntityConfiguration<BackupHistory, BackupHistoryId>
{
    public override void Configure(EntityTypeBuilder<BackupHistory> backupBuilder)
    {
        base.Configure(backupBuilder);

        backupBuilder.HasKey(b => b.Id);

        backupBuilder.ToTable("BackupHistory", DbConfigurationSettings.Schemas.Application);

        backupBuilder
            .Property(b => b.Id)
            .HasConversion<BackupHistoryIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        backupBuilder.Property(b => b.FileName).HasMaxLength(260).IsRequired();

        backupBuilder
            .Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        backupBuilder.Property(b => b.StartedAtUtc).IsRequired();
        backupBuilder.Property(b => b.CompletedAtUtc).IsRequired(false);
        backupBuilder.Property(b => b.SizeBytes).IsRequired(false);
        backupBuilder.Property(b => b.ErrorMessage).HasMaxLength(2000).IsRequired(false);
        backupBuilder.Property(b => b.RetainedUntilUtc).IsRequired(false);

        //* Serves "latest successful backup" (freshness check) and the retention sweep.
        backupBuilder
            .HasIndex(b => new { b.Status, b.StartedAtUtc })
            .HasDatabaseName("IX_BackupHistory_Status_StartedAtUtc");
    }
}
