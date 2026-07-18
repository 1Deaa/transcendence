using HrmSystem.Domain.Entities.Announcements;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Announcements;

internal sealed class AnnouncementConfiguration
    : ATenantEntityConfiguration<Announcement, AnnouncementId>
{
    public override void Configure(EntityTypeBuilder<Announcement> announcementBuilder)
    {
        base.Configure(announcementBuilder); //! auditing + soft delete + IsActive + TenantId

        announcementBuilder.HasKey(a => a.Id);

        announcementBuilder.ToTable(
            DbConfigurationSettings.Tables.Announcements,
            DbConfigurationSettings.Schemas.Application
        );

        announcementBuilder
            .Property(a => a.Id)
            .HasConversion<AnnouncementIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        announcementBuilder
            .Property(a => a.Title)
            .HasMaxLength(AnnouncementErrors.Title.MaxLength)
            .IsRequired();

        announcementBuilder
            .Property(a => a.Body)
            .HasMaxLength(AnnouncementErrors.Body.MaxLength)
            .IsRequired();

        announcementBuilder
            .Property(a => a.AuthorName)
            .HasMaxLength(200)
            .IsRequired();

        announcementBuilder.Property(a => a.PublishedAtUtc).IsRequired();

        #region Optimistic Concurrency

        announcementBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        //? The feed query: newest announcements of a tenant, paged.
        announcementBuilder
            .HasIndex(a => new { a.TenantId, a.PublishedAtUtc })
            .HasDatabaseName("IX_Announcements_TenantId_PublishedAtUtc");
    }
}
