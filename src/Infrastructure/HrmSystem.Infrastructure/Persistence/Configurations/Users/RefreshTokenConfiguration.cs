using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Users;

internal sealed class RefreshTokenConfiguration
    : AFullAuditableEntityConfiguration<RefreshToken, RefreshTokenId>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable(
            DbConfigurationSettings.Tables.RefreshTokens,
            DbConfigurationSettings.Schemas.Application
        );

        // ── Primary Key ───────────────────────────────────────────────────────

        builder.HasKey(rt => rt.Id);

        builder
            .Property(rt => rt.Id)
            .HasConversion<RefreshTokenIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        // ── Token value (owned VO) ────────────────────────────────────────────

        /*
            //*     [OwnsOne] maps the [RefreshTokenValue] VO as a set of inline columns
            //*     on the same [RefreshTokens] table — no join, no separate table.
            //!     The unique index on [Token] makes [FindByTokenValueAsync] an index seek,
            //!     not a full scan — critical for a table that grows with every login.
        */
        builder.OwnsOne(
            rt => rt.Token,
            refreshTokenBuilder =>
            {
                refreshTokenBuilder
                    .Property(t => t.Value)
                    .HasColumnName("Token")
                    .HasMaxLength(RefreshTokenValue.StorageLength)
                    .IsRequired();

                refreshTokenBuilder
                    .HasIndex(t => t.Value)
                    .IsUnique()
                    .HasDatabaseName("UX_RefreshTokens_Token");
            }
        );

        // ── ExpiresOn ─────────────────────────────────────────────────────────

        builder.Property(rt => rt.ExpiresOn).HasColumnName("ExpiresOn").IsRequired();

        // ── IsSuperseded ──────────────────────────────────────────────────────

        /*
            //?     [false] on a fresh token; set to [true] by [Supersede()] during rotation.
            //?     Superseded rows stay in the table as reuse-detection tripwires.
            //!     Never soft-delete a superseded token immediately — it must remain
            //!     queryable so [FindByTokenValueAsync] can detect if the old value is resubmitted.
        */
        builder
            .Property(rt => rt.IsSuperseded)
            .HasColumnName("IsSuperseded")
            .HasDefaultValue(false)
            .IsRequired();

        // ── Foreign Key: UserId → User ────────────────────────────────────────

        /*
            //?     [UserId] is a [UserId] VO — [UserIdConverter] maps it to/from the string
            //?     column type that matches [User.Id]'s column, enabling the FK relationship.
            //
            //*     [HasForeignKey] references the strongly-typed VO property; EF Core matches
            //*     it to [User.Id] via the shared [UserIdConverter] on both ends.
            //
            //!     [OnDelete(Cascade)]: deleting a [User] hard-deletes all their refresh tokens.
            //!     This is intentional — a deleted user cannot re-authenticate, so their tokens
            //!     should not linger. Soft-delete on [User] keeps the FK intact.
        */
        builder
            .Property(rt => rt.UserId)
            .HasConversion<UserIdConverter>()
            .HasColumnName("UserId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        builder
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Index: UserId ─────────────────────────────────────────────────────

        //? Speeds up [FindByUserIdAsync] and cascade-delete lookups.
        builder.HasIndex(rt => rt.UserId).HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
