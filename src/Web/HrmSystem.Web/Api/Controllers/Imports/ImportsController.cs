using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Imports.CreateImportJob;
using HrmSystem.Application.Features.Imports.GetImportJobById;
using HrmSystem.Application.Features.Imports.GetImportJobs;
using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Imports;

/*
    //*     CSV import pipeline (DevHabit pattern).
    //>     Routes owned here:   POST /api/imports/employees   (multipart upload → 202 + Location)
    //>                          GET  /api/imports/{importJobId}   (poll status/progress)
    //>                          GET  /api/imports                 (paged history)
*/
[Route("api/imports")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ImportsController(ISender sender) : ApiBaseController
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>Uploads an employee CSV and starts background processing.</summary>
    /// <remarks>Expected columns: FirstName,LastName,Email,JobTitle,DepartmentCode,HiredOn (yyyy-MM-dd).</remarks>
    [HttpPost("employees")]
    [HasPermission(Permissions.Imports.Write)]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ImportEmployees(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        //! Cheap transport-level checks here; the domain re-validates content authoritatively.
        if (file.Length == 0)
        {
            return Problem(
                [Error.Validation("ImportJob.FileRequired", "A non-empty CSV file is required.")]
            );
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                [Error.Validation("ImportJob.CsvOnly", "Only .csv files are accepted.")]
            );
        }

        byte[] content;
        await using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            content = memoryStream.ToArray();
        }

        Result<string> result = await sender.Send(
            new CreateImportJobCommand(file.FileName, content),
            cancellationToken
        );

        //? 202 — processing happens in the background; Location points at the polling resource.
        return result.Match<IActionResult>(
            importJobId =>
                AcceptedAtAction(nameof(GetImportJobById), new { importJobId }, importJobId),
            Problem
        );
    }

    /// <summary>Polls an import job's status, counters and row-level errors.</summary>
    [HttpGet("{importJobId}")]
    [HasPermission(Permissions.Imports.Read)]
    [ProducesResponseType<ImportJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImportJobById(
        string importJobId,
        CancellationToken cancellationToken
    )
    {
        Result<ImportJobResponse> result = await sender.Send(
            new GetImportJobByIdQuery(importJobId),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Lists import jobs, newest first.</summary>
    [HttpGet]
    [HasPermission(Permissions.Imports.Read)]
    [ProducesResponseType<PaginationResult<ImportJobResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        Result<PaginationResult<ImportJobResponse>> result = await sender.Send(
            new GetImportJobsQuery(page, pageSize),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }
}
