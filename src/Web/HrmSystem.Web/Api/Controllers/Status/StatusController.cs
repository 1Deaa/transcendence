using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Features.Status.GetComponentHealth;
using HrmSystem.Application.Features.Status.GetPublicStatus;
using HrmSystem.Application.Features.Status.GetUptimeHistory;
using HrmSystem.Application.Features.Status.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Status;

/*
    //*     Status page resources.
    //>     Routes owned here:   GET /api/status                    (anonymous summary)
    //>                          GET /api/status/history?days=30    (anonymous uptime series)
    //>                          GET /api/status/components         (health:read — full detail)
    //
    //!     A Degraded system still answers 200 here — a status page that 503s during the
    //!     incident it reports is useless. Raw liveness/readiness live at /health[.ready].
*/
[Route("api/status")]
[ApiController]
public sealed class StatusController(ISender sender) : ApiBaseController
{
    /// <summary>Public status summary — overall + per-component state, uptime, last backup.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PublicStatusResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicStatus(CancellationToken cancellationToken)
    {
        Result<PublicStatusResponse> result = await sender.Send(
            new GetPublicStatusQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Public per-day uptime history for the status-page chart.</summary>
    [HttpGet("history")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<UptimePoint>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUptimeHistory(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default
    )
    {
        Result<IReadOnlyList<UptimePoint>> result = await sender.Send(
            new GetUptimeHistoryQuery(days),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Detailed component health — probe durations, descriptions, exception text.</summary>
    [HttpGet("components")]
    [HasPermission(Permissions.Health.Read)]
    [ProducesResponseType<IReadOnlyList<ComponentHealthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetComponentHealth(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ComponentHealthResponse>> result = await sender.Send(
            new GetComponentHealthQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }
}
