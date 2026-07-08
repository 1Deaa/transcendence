using HrmSystem.Domain.Entities.EmailTemplates;
using HrmSystem.Domain.Entities.EmailTemplates.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.EmailTemplates;

internal sealed class EmailTemplateConfiguration
    : ASoftDeletableAuditableEntityConfiguration<EmailTemplate, EmailTemplateId>
{
    public override void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        base.Configure(builder);

        builder.HasKey(e => e.Id);

        builder.ToTable(
            DbConfigurationSettings.Tables.EmailTemplates,
            DbConfigurationSettings.Schemas.Application
        );

        builder
            .Property(e => e.Id)
            .HasConversion<EmailTemplateIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        /*
        //?  EmailTemplateKey is a smart enum — stored as its string Value (e.g. "Habit.Created").
        //?  The converter validates on read: if the DB holds an unknown key, it throws rather
        //?  than silently returning null, making stale data loud and obvious.
        //
        //!  Unique index below enforces one row per key — the table is a lookup, not a log.
        */
        builder
            .Property(e => e.Key)
            .HasConversion<EmailTemplateKeyConverter>()
            .HasColumnName("Key")
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(
            e => e.Subject,
            subjectBuilder =>
                subjectBuilder
                    .Property(s => s.Value)
                    .HasColumnName("Subject")
                    .HasMaxLength(EmailTemplateSubject.MaxLength)
                    .IsRequired()
        );

        builder.OwnsOne(
            e => e.Body,
            bodyBuilder =>
                bodyBuilder
                    .Property(b => b.Value)
                    .HasColumnName("Body")
                    .HasMaxLength(EmailTemplateBody.MaxLength)
                    .IsRequired()
        );

        builder.HasIndex(e => e.Key).IsUnique().HasDatabaseName("UQ_EmailTemplates_Key");
    }
}
