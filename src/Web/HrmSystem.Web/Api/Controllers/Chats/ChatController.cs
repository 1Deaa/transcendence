using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Chats.GetChatUsers;
using HrmSystem.Application.Features.Chats.GetConversation;
using HrmSystem.Application.Features.Chats.GetConversations;
using HrmSystem.Application.Features.Chats.MarkConversationRead;
using HrmSystem.Application.Features.Chats.SendChatMessage;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Chats;

/*
    //*     Direct messages between users of the same workspace, delivered live over
    //*     the notifications hub ("ChatMessageReceived" to both participants).
    //>     Routes owned here:   GET  /api/chat/conversations                     (sidebar)
    //>                          GET  /api/chat/users                             (directory)
    //>                          GET  /api/chat/conversations/{userId}/messages   (thread, paged)
    //>                          POST /api/chat/messages                          (send)
    //>                          POST /api/chat/conversations/{userId}/read       (read receipts)
*/
[Route("api/chat")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ChatController(ISender sender) : ApiBaseController
{
    /// <summary>Lists the caller's conversations with previews and unread counts.</summary>
    [HttpGet("conversations")]
    [HasPermission(Permissions.Chat.Read)]
    [ProducesResponseType<IReadOnlyList<ConversationSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ConversationSummary>> result = await sender.Send(
            new GetConversationsQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Lists every user of the caller's workspace (for starting a conversation).</summary>
    [HttpGet("users")]
    [HasPermission(Permissions.Chat.Read)]
    [ProducesResponseType<IReadOnlyList<ChatUserSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatUsers(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ChatUserSummary>> result = await sender.Send(
            new GetChatUsersQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Pages through one conversation, newest messages first.</summary>
    [HttpGet("conversations/{userId}/messages")]
    [HasPermission(Permissions.Chat.Read)]
    [ProducesResponseType<PaginationResult<ChatMessageResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default
    )
    {
        Result<PaginationResult<ChatMessageResponse>> result = await sender.Send(
            new GetConversationQuery(userId, page, pageSize),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Sends a direct message; both participants receive it over SignalR instantly.</summary>
    [HttpPost("messages")]
    [HasPermission(Permissions.Chat.Write)]
    [ProducesResponseType<ChatMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<ChatMessageResponse> result = await sender.Send(
            new SendChatMessageCommand(request.RecipientUserId, request.Content),
            cancellationToken
        );

        return result.Match<IActionResult>(
            message => CreatedAtAction(
                nameof(GetConversation),
                routeValues: new { userId = message.RecipientUserId },
                message
            ),
            Problem
        );
    }

    /// <summary>Marks every unread message from that user as read.</summary>
    [HttpPost("conversations/{userId}/read")]
    [HasPermission(Permissions.Chat.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkConversationRead(
        [FromRoute] string userId,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new MarkConversationReadCommand(userId),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }
}
