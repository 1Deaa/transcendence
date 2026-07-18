using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Chats.Shared;

namespace HrmSystem.Application.Features.Chats.GetChatUsers;

//? Workspace directory (everyone except the caller) — powers the "new message" picker.
public sealed record GetChatUsersQuery : IQuery<IReadOnlyList<ChatUserSummary>>;
