using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Features.Attendances.ClockIn;
using HrmSystem.Application.Features.Attendances.ClockOut;
using HrmSystem.Application.Features.Attendances.GetAttendanceForEmployee;
using HrmSystem.Application.Features.Attendances.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Attendances;

/*
    //*     Attendance resource — clock in/out plus per-employee history.
    //>     Routes owned here:   POST /api/attendances/clock-in
    //>                          POST /api/attendances/clock-out
    //>                          GET  /api/attendances/employees/{employeeId}
*/
[Route("api/attendances")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AttendancesController(ISender sender) : ApiBaseController
{
    /// <summary>Clocks an employee in for today's working day.</summary>
    [HttpPost("clock-in")]
    [HasPermission(Permissions.Attendance.Write)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClockIn(
        [FromBody] ClockRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<string> result = await sender.Send(
            new ClockInCommand(request.EmployeeId),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Clocks an employee out of today's working day.</summary>
    [HttpPost("clock-out")]
    [HasPermission(Permissions.Attendance.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClockOut(
        [FromBody] ClockRequest request,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new ClockOutCommand(request.EmployeeId),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Gets an employee's attendance history for an inclusive date window.</summary>
    [HttpGet("employees/{employeeId}")]
    [HasPermission(Permissions.Attendance.Read)]
    [ProducesResponseType<IReadOnlyList<AttendanceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceForEmployee(
        string employeeId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken
    )
    {
        Result<IReadOnlyList<AttendanceResponse>> result = await sender.Send(
            new GetAttendanceForEmployeeQuery(employeeId, from, to),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }
}
