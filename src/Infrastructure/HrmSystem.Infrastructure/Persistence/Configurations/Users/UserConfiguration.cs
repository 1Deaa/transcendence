using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using HrmSystem.Infrastructure.Services.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Users;

internal sealed class UserConfiguration : AFullAuditableEntityConfiguration<User, UserId>
{
    public override void Configure(EntityTypeBuilder<User> userBuilder)
    {
        base.Configure(userBuilder);

        userBuilder.HasKey(u => u.Id);

        userBuilder.ToTable(
            DbConfigurationSettings.Tables.Users,
            DbConfigurationSettings.Schemas.Application
        );

        userBuilder
            .Property(u => u.Id)
            .HasConversion<UserIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        /*
            //?  1:1 relationship — [User] (dependent) → [AppUser] (principal).
            //?  [AppUser] is registered in [ApplicationDbContext] with [ExcludeFromMigrations()]
            //?  so EF Core knows the shape without trying to create the Identity table again.
            //
            //*  [HasForeignKey<User>] puts the FK column on the [User] side ([IdentityId]).
            //*  [HasPrincipalKey<AppUser>] points to [AppUser.Id] (the Identity PK).
            //!  [DeleteBehavior.Restrict] — deleting an [AppUser] while a domain [User]
            //!  still references it is blocked at the DB level.
        */
        userBuilder
            .Property(u => u.IdentityId)
            .HasColumnName("IdentityId")
            /*
                //!  Must be 450 — not [MaxUUIDv7MaxLength].
                //!  ASP.NET Core Identity stores [AppUser.Id] as [nvarchar(450)] by default.
                //!  SQL Server requires FK columns to be identical in length to the referenced PK.
            */
            .HasMaxLength(450)
            .IsRequired();

        userBuilder.HasIndex(u => u.IdentityId).IsUnique().HasDatabaseName("UX_Users_IdentityId");

        /*
            //?     Workspace membership — stamped into the JWT as "tenant_id" at login.
            //!     Nullable: host-level platform admins have no tenant. No FK constraint on purpose —
            //!     User is host-level data and must survive tenant archival/restore operations.
        */
        userBuilder
            .Property(u => u.TenantId)
            .HasConversion<Tenants.TenantIdConverter>()
            .HasColumnName("TenantId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired(false);

        userBuilder.HasIndex(u => u.TenantId).HasDatabaseName("IX_Users_TenantId");

        #region Owned Value Objects

        userBuilder.OwnsOne(
            user => user.UserName,
            userNameBuilder =>
            {
                userNameBuilder
                    .Property(n => n.Value)
                    .HasColumnName("UserName")
                    .HasMaxLength(UserName.MaxLength)
                    .IsRequired();

                userNameBuilder
                    .HasIndex(n => n.Value)
                    .IsUnique()
                    .HasDatabaseName("UX_Users_UserName");
            }
        );

        userBuilder.OwnsOne(
            user => user.FirstName,
            firstNameBuilder =>
            {
                firstNameBuilder
                    .Property(fn => fn.Value)
                    .HasColumnName("FirstName")
                    .HasMaxLength(FirstName.MaxLength)
                    .IsRequired();
            }
        );

        userBuilder.OwnsOne(
            user => user.LastName,
            lastNameBuilder =>
            {
                lastNameBuilder
                    .Property(ln => ln.Value)
                    .HasColumnName("LastName")
                    .HasMaxLength(LastName.MaxLength)
                    .IsRequired();
            }
        );

        userBuilder.OwnsOne(
            user => user.MiddleName,
            middleNameBuilder =>
            {
                middleNameBuilder
                    .Property(mn => mn.Value)
                    .HasColumnName("MiddleName")
                    .HasMaxLength(MiddleName.MaxLength)
                    .IsRequired(false);
            }
        );

        userBuilder.OwnsOne(
            user => user.Email,
            emailBuilder =>
            {
                emailBuilder
                    .Property(email => email.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(Email.MaxLength)
                    .IsRequired();

                emailBuilder.HasIndex(e => e.Value).IsUnique().HasDatabaseName("UX_Users_Email");
            }
        );

        userBuilder.OwnsOne(
            user => user.PhoneNumber,
            phoneNumberBuilder =>
            {
                phoneNumberBuilder
                    .Property(phoneNumber => phoneNumber.Value)
                    .HasColumnName("PhoneNumber")
                    .HasMaxLength(PhoneNumber.MaxLength)
                    .IsRequired(false);

                phoneNumberBuilder
                    .HasIndex(pn => pn.Value)
                    .IsUnique()
                    .HasDatabaseName("UX_Users_PhoneNumber");
            }
        );

        userBuilder.OwnsOne(
            user => user.SecondaryEmail,
            secondaryEmailBuilder =>
            {
                secondaryEmailBuilder
                    .Property(secondaryEmail => secondaryEmail.Value)
                    .HasColumnName("SecondaryEmail")
                    .HasMaxLength(SecondaryEmail.MaxLength)
                    .IsRequired(false);
            }
        );

        userBuilder.OwnsOne(
            user => user.SecondaryPhoneNumber,
            secondaryPhoneNumberBuilder =>
            {
                secondaryPhoneNumberBuilder
                    .Property(secondaryPhoneNumber => secondaryPhoneNumber.Value)
                    .HasColumnName("SecondaryPhoneNumber")
                    .HasMaxLength(SecondaryPhoneNumber.MaxLength)
                    .IsRequired(false);
            }
        );

        #endregion Owned Value Objects

        #region Indexes && Unique Constraints


        //! Indexes are defined inside each OwnsOne block — EF Core cannot index an owned
        //! navigation from the outer builder (it registers it as a property first, then
        //! OwnsOne tries to register it again as a navigation → duplicate name conflict).

        // TODO: //!    - Add Index for [IdentityId] after adding it
        //userBuilder
        //    .HasIndex(u => u.IdentityId)
        //    .IsUnique()
        //    .HasFilter("[IdentityId] IS NOT NULL")
        //    .HasDatabaseName("UX_Users_IdentityId");

        #endregion Indexes && Unique Constraints

        #region Navigations & Relations

        userBuilder
            .HasOne<AppUser>()
            .WithOne(appUser => appUser.DomainUser)
            .HasForeignKey<User>(u => u.IdentityId)
            .HasPrincipalKey<AppUser>(appUser => appUser.Id)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion Navigations & Relations
    }
}
