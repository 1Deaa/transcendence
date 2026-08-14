using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Backups.GetBackupHistory;
using HrmSystem.Application.Features.Backups.Shared;
using HrmSystem.Application.Features.Backups.TriggerBackup;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Backups;

/*
    //*     Host-level backup administration.
    //>     Routes owned here:   GET  /api/backups        (backups:read — paged history)
    //>                          POST /api/backups/run    (backups:manage — trigger now)
*/
[Route("api/backups")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class BackupsController(ISender sender) : ApiBaseController
{
    /// <summary>Lists backup history, newest first.</summary>
    [HttpGet]
    [HasPermission(Permissions.Backups.Read)]
    [ProducesResponseType<PaginationResult<BackupHistoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBackupHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        Result<PaginationResult<BackupHistoryResponse>> result = await sender.Send(
            new GetBackupHistoryQuery(page, pageSize),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Triggers an immediate database backup run.</summary>
    /// <remarks>Returns 503 when the job scheduler is down (ErrorType.Unavailable).</remarks>
    [HttpPost("run")]
    [HasPermission(Permissions.Backups.Manage)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TriggerBackup(CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new TriggerBackupCommand(), cancellationToken);

        //? 202 — the backup runs in the background; poll GET /api/backups for the outcome.
        return result.Match<IActionResult>(Accepted, Problem);
    }
}
