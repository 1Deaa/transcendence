using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Chats.MarkConversationRead;

//? Marks every unread message FROM [OtherUserId] TO the current user as read.
public sealed record MarkConversationReadCommand(string OtherUserId) : ICommand;
