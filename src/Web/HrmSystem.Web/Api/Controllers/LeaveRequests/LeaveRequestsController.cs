using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.LeaveRequests.ApproveLeaveRequest;
using HrmSystem.Application.Features.LeaveRequests.GetLeaveRequests;
using HrmSystem.Application.Features.LeaveRequests.RejectLeaveRequest;
using HrmSystem.Application.Features.LeaveRequests.Shared;
using HrmSystem.Application.Features.LeaveRequests.SubmitLeaveRequest;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.LeaveRequests;

/*
    //*     Leave-request resource — submission plus the manager decision workflow.
    //>     Routes owned here:   GET  /api/leave-requests                    (paged + filters)
    //>                          POST /api/leave-requests
    //>                          POST /api/leave-requests/{leaveRequestId}/approve
    //>                          POST /api/leave-requests/{leaveRequestId}/reject
*/
[Route("api/leave-requests")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class LeaveRequestsController(ISender sender) : ApiBaseController
{
    /// <summary>Lists leave requests with paging and filters.</summary>
    [HttpGet]
    [HasPermission(Permissions.Leaves.Read)]
    [ProducesResponseType<PaginationResult<LeaveRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? employeeId = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetLeaveRequestsQuery(page, pageSize, status, employeeId);

        Result<PaginationResult<LeaveRequestResponse>> result = await sender.Send(
            query,
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Submits a new leave request for an employee.</summary>
    [HttpPost]
    [HasPermission(Permissions.Leaves.Write)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitLeaveRequest(
        [FromBody] SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new SubmitLeaveRequestCommand(
            request.EmployeeId,
            request.Type,
            request.Start,
            request.End,
            request.Reason
        );

        Result<string> result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            leaveRequestId =>
                CreatedAtAction(nameof(GetLeaveRequests), new { id = leaveRequestId }, leaveRequestId),
            Problem
        );
    }

    /// <summary>Approves a pending leave request.</summary>
    [HttpPost("{leaveRequestId}/approve")]
    [HasPermission(Permissions.Leaves.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveLeaveRequest(
        string leaveRequestId,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new ApproveLeaveRequestCommand(leaveRequestId),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Rejects a pending leave request.</summary>
    [HttpPost("{leaveRequestId}/reject")]
    [HasPermission(Permissions.Leaves.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectLeaveRequest(
        string leaveRequestId,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new RejectLeaveRequestCommand(leaveRequestId),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }
}
