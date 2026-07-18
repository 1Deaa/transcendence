using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Chats.Shared;

namespace HrmSystem.Application.Features.Chats.GetConversations;

//? The chat sidebar: one row per conversation partner with preview + unread count.
public sealed record GetConversationsQuery : IQuery<IReadOnlyList<ConversationSummary>>;
