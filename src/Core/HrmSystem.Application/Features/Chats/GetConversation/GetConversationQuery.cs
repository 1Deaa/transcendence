using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Chats.Shared;

namespace HrmSystem.Application.Features.Chats.GetConversation;

//? Messages between the current user and [OtherUserId], newest first, offset-paged.
public sealed record GetConversationQuery(string OtherUserId, int Page, int PageSize)
    : IQuery<PaginationResult<ChatMessageResponse>>;
