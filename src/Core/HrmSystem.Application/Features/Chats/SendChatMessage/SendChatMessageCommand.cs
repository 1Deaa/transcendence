using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Chats.Shared;

namespace HrmSystem.Application.Features.Chats.SendChatMessage;

//? Sender comes from the authenticated session — the client can only pick the recipient.
public sealed record SendChatMessageCommand(string RecipientUserId, string Content)
    : ICommand<ChatMessageResponse>;
