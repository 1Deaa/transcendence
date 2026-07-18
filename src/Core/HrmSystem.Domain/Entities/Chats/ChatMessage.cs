using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Chats.Events;
using HrmSystem.Domain.Entities.Chats.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Domain.Entities.Chats;

/*
    //?     The ChatMessage aggregate root — TENANT-OWNED (ATenantEntity → tenant query
    //?     filter + write guard). One direct message between two users of the same
    //?     workspace, delivered in real time (ChatMessageSentDomainEvent → SignalR).
    //!     Message text is immutable after send — edits would rewrite a conversation the
    //!     other side already read. Read state is the ONLY mutable field.
*/
public class ChatMessage : ATenantEntity<ChatMessageId>
{
    #region Constructor

    private ChatMessage(
        ChatMessageId id,
        UserId senderUserId,
        UserId recipientUserId,
        string content,
        DateTime sentAtUtc
    )
        : base(id)
    {
        SenderUserId = senderUserId;
        RecipientUserId = recipientUserId;
        Content = content;
        SentAtUtc = sentAtUtc;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private ChatMessage()
        : base() { }

    #endregion

    #region Properties

    public UserId SenderUserId { get; private set; } = null!;

    public UserId RecipientUserId { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    public DateTime SentAtUtc { get; private set; }

    //? Null until the recipient opens the conversation — powers the unread counters.
    public DateTime? ReadAtUtc { get; private set; }

    #endregion

    #region Static factory

    //? The only way to create a valid ChatMessage — collects every broken rule before returning.
    public static Result<ChatMessage> Send(
        UserId senderUserId,
        UserId recipientUserId,
        string content,
        DateTime utcNow
    )
    {
        var errors = new List<Error>();

        string trimmedContent = content?.Trim() ?? string.Empty;
        if (trimmedContent.Length == 0)
        {
            errors.Add(ChatMessageErrors.Content.Required);
        }
        else if (trimmedContent.Length > ChatMessageErrors.Content.MaxLength)
        {
            errors.Add(ChatMessageErrors.Content.TooLong);
        }

        if (senderUserId.Value == recipientUserId.Value)
        {
            errors.Add(ChatMessageErrors.Recipient.SameAsSender);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var message = new ChatMessage(
            ChatMessageId.New(),
            senderUserId,
            recipientUserId,
            trimmedContent,
            utcNow
        );

        message.RaiseDomainEvent(new ChatMessageSentDomainEvent(message.Id!));

        return message;
    }

    #endregion

    #region Methods

    //? Idempotent — marking an already-read message again keeps the FIRST read timestamp.
    public void MarkAsRead(DateTime utcNow)
    {
        ReadAtUtc ??= utcNow;
    }

    #endregion
}
