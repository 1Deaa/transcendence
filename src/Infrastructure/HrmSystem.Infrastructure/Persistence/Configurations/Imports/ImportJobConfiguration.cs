using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Imports;

internal sealed class ImportJobIdConverter : ValueConverter<ImportJobId, string>
{
    public ImportJobIdConverter()
        : base(id => id.Value, rawStr => ConvertToId(rawStr)) { }

    private static ImportJobId ConvertToId(string rawStr)
    {
        Result<ImportJobId> result = ImportJobId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted ImportJobId: '{rawStr}'."
            );
        }

        return result.Value;
    }
}

internal sealed class ImportJobConfiguration : ATenantEntityConfiguration<ImportJob, ImportJobId>
{
    public override void Configure(EntityTypeBuilder<ImportJob> importBuilder)
    {
        base.Configure(importBuilder); //! auditing + soft delete + IsActive + TenantId

        importBuilder.HasKey(i => i.Id);

        importBuilder.ToTable("ImportJobs", DbConfigurationSettings.Schemas.Application);

        importBuilder
            .Property(i => i.Id)
            .HasConversion<ImportJobIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        importBuilder
            .Property(i => i.EntityType)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        importBuilder
            .Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(DomainConstants.MaxEnumLength)
            .IsRequired();

        importBuilder.Property(i => i.FileName).HasMaxLength(260).IsRequired();

        //! varbinary(max) — the uploaded CSV rides in the row (DevHabit pattern) so the
        //! Quartz worker needs no shared filesystem; cleared on Complete()/MarkFailed().
        importBuilder.Property(i => i.FileContent).IsRequired();

        importBuilder.Property(i => i.TotalRecords).IsRequired();
        importBuilder.Property(i => i.ProcessedRecords).IsRequired();
        importBuilder.Property(i => i.SuccessfulRecords).IsRequired();
        importBuilder.Property(i => i.FailedRecords).IsRequired();
        importBuilder.Property(i => i.LoggedErrorCount).IsRequired();
        importBuilder.Property(i => i.ErrorLog).IsRequired();
        importBuilder.Property(i => i.CompletedAtUtc).IsRequired(false);

        #region Optimistic Concurrency

        importBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        //* Serves the imports dashboard (newest first per tenant) and the stale-Pending sweep.
        importBuilder
            .HasIndex(i => new { i.TenantId, i.Status })
            .HasDatabaseName("IX_ImportJobs_TenantId_Status");
    }
}
