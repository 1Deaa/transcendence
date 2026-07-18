using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Chats.ValueObjects;
using HrmSystem.Domain.Shared;
using HrmSystem.Infrastructure.Persistence.Configurations.Base;
using HrmSystem.Infrastructure.Persistence.Configurations.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Chats;

internal sealed class ChatMessageConfiguration
    : ATenantEntityConfiguration<ChatMessage, ChatMessageId>
{
    public override void Configure(EntityTypeBuilder<ChatMessage> chatMessageBuilder)
    {
        base.Configure(chatMessageBuilder); //! auditing + soft delete + IsActive + TenantId

        chatMessageBuilder.HasKey(m => m.Id);

        chatMessageBuilder.ToTable(
            DbConfigurationSettings.Tables.ChatMessages,
            DbConfigurationSettings.Schemas.Application
        );

        chatMessageBuilder
            .Property(m => m.Id)
            .HasConversion<ChatMessageIdConverter>()
            .HasColumnName("Id")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        /*
            //!     No FK constraints to [Users] on purpose — Users are host-level rows and
            //!     chat history must survive account archival; consistency is enforced by
            //!     the SendChatMessage handler (same-workspace recipient check).
        */
        chatMessageBuilder
            .Property(m => m.SenderUserId)
            .HasConversion<UserIdConverter>()
            .HasColumnName("SenderUserId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        chatMessageBuilder
            .Property(m => m.RecipientUserId)
            .HasConversion<UserIdConverter>()
            .HasColumnName("RecipientUserId")
            .HasMaxLength(DomainConstants.MaxUUIDv7MaxLength)
            .IsRequired();

        chatMessageBuilder
            .Property(m => m.Content)
            .HasMaxLength(ChatMessageErrors.Content.MaxLength)
            .IsRequired();

        chatMessageBuilder.Property(m => m.SentAtUtc).IsRequired();

        chatMessageBuilder.Property(m => m.ReadAtUtc).IsRequired(false);

        #region Optimistic Concurrency

        chatMessageBuilder.Property<byte[]>("RowVersion").IsRowVersion();

        #endregion

        //? Conversation fetch: both directions of a pair, ordered by time.
        chatMessageBuilder
            .HasIndex(m => new { m.TenantId, m.SenderUserId, m.RecipientUserId, m.SentAtUtc })
            .HasDatabaseName("IX_ChatMessages_TenantId_Sender_Recipient_SentAtUtc");

        //? Unread counters: incoming messages of a recipient still awaiting a read receipt.
        chatMessageBuilder
            .HasIndex(m => new { m.TenantId, m.RecipientUserId, m.ReadAtUtc })
            .HasDatabaseName("IX_ChatMessages_TenantId_Recipient_ReadAtUtc");
    }
}
