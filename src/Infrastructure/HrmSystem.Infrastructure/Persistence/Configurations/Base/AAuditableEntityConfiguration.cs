using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Base;

internal abstract class AAuditableEntityConfiguration<TEntity, TId>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : AAuditableEntity<TId>
    where TId : class
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        int MaxLengthForNames =
            UserName.MaxLength
            + FirstName.MaxLength
            + MiddleName.MaxLength
            + LastName.MaxLength
            + 10; //! for spaces or dashes (-) and parentheses

        builder.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.CreatedBy).HasMaxLength(MaxLengthForNames).IsRequired(false);

        builder.Property(a => a.LastModifiedAt).IsRequired(false);

        builder.Property(a => a.LastModifiedBy).HasMaxLength(MaxLengthForNames).IsRequired(false);
    }
}
