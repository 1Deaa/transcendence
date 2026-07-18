using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Announcements.GetAnnouncements;
using HrmSystem.Application.Features.Announcements.PublishAnnouncement;
using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Announcements;

/*
    //*     Announcement resource — the tenant-wide message board.
    //>     Routes owned here:   GET  /api/announcements        (paged feed, newest first)
    //>                          POST /api/announcements        (publish → SignalR fan-out)
    //
    //?     Publishing raises AnnouncementPublishedDomainEvent, which the NotificationsHub
    //?     broadcasts to every connected member of the tenant (see AnnouncementNotifier).
*/
[Route("api/announcements")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AnnouncementsController(ISender sender) : ApiBaseController
{
    /// <summary>Pages through the tenant's announcements, newest first.</summary>
    [HttpGet]
    [HasPermission(Permissions.Announcements.Read)]
    [ProducesResponseType<PaginationResult<AnnouncementResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        Result<PaginationResult<AnnouncementResponse>> result = await sender.Send(
            new GetAnnouncementsQuery(page, pageSize),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Publishes a company-wide announcement and pushes it to all connected employees.</summary>
    [HttpPost]
    [HasPermission(Permissions.Announcements.Write)]
    [ProducesResponseType<AnnouncementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishAnnouncement(
        [FromBody] PublishAnnouncementRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<AnnouncementResponse> result = await sender.Send(
            new PublishAnnouncementCommand(request.Title, request.Body),
            cancellationToken
        );

        return result.Match<IActionResult>(
            announcement => CreatedAtAction(
                nameof(GetAnnouncements),
                routeValues: null,
                announcement
            ),
            Problem
        );
    }
}
