using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Chats.SendChatMessage;

/*
    //?     The recipient is verified to be a USER OF THE SAME WORKSPACE before anything is
    //?     written — the domain [User] is host-level (no tenant query filter), so the
    //?     tenant check here is explicit and mandatory.
    //?     The ChatMessageSentDomainEvent raised by the factory fires AFTER SaveChangesAsync
    //?     and fans the message out over SignalR to both participants.
*/
internal sealed class SendChatMessageCommandHandler(
    IChatMessageRepository chatMessageRepository,
    IUserRepository userRepository,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<SendChatMessageCommand, ChatMessageResponse>
{
    public async Task<Result<ChatMessageResponse>> Handle(
        SendChatMessageCommand command,
        CancellationToken cancellationToken
    )
    {
        UserId? senderUserId = currentUserContext.DomainUserId;
        if (senderUserId is null)
        {
            return ChatMessageErrors.Sender.Unresolved;
        }

        Result<UserId> recipientIdResult = UserId.From(command.RecipientUserId);
        if (recipientIdResult.IsFailure)
        {
            return recipientIdResult.Errors;
        }

        User? recipient = await userRepository.GetByIdAsync(
            recipientIdResult.Value,
            cancellationToken
        );

        /*
            //!     Cross-tenant probe defense: a recipient outside the caller's workspace is
            //!     reported as NOT FOUND — never as "exists but foreign".
        */
        bool recipientInWorkspace =
            recipient is not null
            && recipient.TenantId is not null
            && recipient.TenantId.Value == tenantContext.Current?.Value;

        if (!recipientInWorkspace)
        {
            return ChatMessageErrors.Recipient.NotInWorkspace;
        }

        Result<ChatMessage> messageResult = ChatMessage.Send(
            senderUserId,
            recipientIdResult.Value,
            command.Content,
            dateTimeProvider.UtcNow
        );

        if (messageResult.IsFailure)
        {
            return messageResult.Errors.ToList();
        }

        await chatMessageRepository.AddAsync(messageResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ChatMessageResponse.FromMessage(messageResult.Value);
    }
}
