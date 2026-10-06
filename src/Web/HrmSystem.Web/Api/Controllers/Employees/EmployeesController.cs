using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Employees.BulkDeleteEmployees;
using HrmSystem.Application.Features.Employees.BulkHireEmployees;
using HrmSystem.Application.Features.Employees.ExportEmployees;
using HrmSystem.Application.Features.Employees.GetAllEmployees;
using HrmSystem.Application.Features.Employees.GetEmployeeById;
using HrmSystem.Application.Features.Employees.GetMyEmployee;
using HrmSystem.Application.Features.Employees.HireEmployee;
using HrmSystem.Application.Features.Employees.Shared;
using HrmSystem.Application.Features.Employees.TerminateEmployee;
using HrmSystem.Application.Features.Employees.UpdateEmployee;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Employees;

/*
    //*     Employee resource — tenant-scoped CRUD + employment transitions.
    //>     Routes owned here:   GET    /api/employees                    (paged + filters)
    //>                          GET    /api/employees/{employeeId}
    //>                          POST   /api/employees                    (hire)
    //>                          PUT    /api/employees/{employeeId}
    //>                          POST   /api/employees/{employeeId}/terminate
*/
[Route("api/employees")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class EmployeesController(ISender sender) : ApiBaseController
{
    /// <summary>Lists employees with paging, search and filters.</summary>
    [HttpGet]
    [HasPermission(Permissions.Employees.Read)]
    [ProducesResponseType<PaginationResult<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery(Name = "q")] string? searchTerm = null,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetAllEmployeesQuery(
            page,
            pageSize,
            searchTerm,
            departmentId,
            status,
            sortBy,
            sortDirection
        );

        Result<PaginationResult<EmployeeResponse>> result = await sender.Send(
            query,
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /*
        //?     The caller's own employee record, matched by the email claim — needs only
        //?     authentication, NOT employees:read, so Employee-role users can identify
        //?     themselves (e.g. to submit their own leave request).
        //!     The literal "me" template outranks the "{employeeId}" template below.
    */
    /// <summary>Gets the employee record belonging to the current user.</summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.ApiUser)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEmployee(CancellationToken cancellationToken)
    {
        Result<EmployeeResponse> result = await sender.Send(
            new GetMyEmployeeQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Gets a single employee by id.</summary>
    [HttpGet("{employeeId}")]
    [HasPermission(Permissions.Employees.Read)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(
        string employeeId,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeResponse> result = await sender.Send(
            new GetEmployeeByIdQuery(employeeId),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Hires (creates) a new employee.</summary>
    [HttpPost]
    [HasPermission(Permissions.Employees.Write)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HireEmployee(
        [FromBody] HireEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new HireEmployeeCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.JobTitle,
            request.DepartmentId,
            request.HiredOn
        );

        Result<string> result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            employeeId => CreatedAtAction(nameof(GetEmployeeById), new { employeeId }, employeeId),
            Problem
        );
    }

    /// <summary>Updates an employee's profile and department.</summary>
    [HttpPut("{employeeId}")]
    [HasPermission(Permissions.Employees.Modify)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmployee(
        string employeeId,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateEmployeeCommand(
            employeeId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.JobTitle,
            request.DepartmentId
        );

        Result result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Exports all employees — format via Accept header or ?format=json|xml|csv.</summary>
    [HttpGet("export")]
    [FormatFilter]
    [HasPermission(Permissions.Exports.Read)]
    [ProducesResponseType<IReadOnlyList<EmployeeExportRow>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportEmployees(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EmployeeExportRow>> result = await sender.Send(
            new ExportEmployeesQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Hires up to 500 employees in one batch — per-item Result reporting.</summary>
    [HttpPost("bulk")]
    [HasPermission(Permissions.Employees.Write)]
    [ProducesResponseType<BulkOperationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkHireEmployees(
        [FromBody] IReadOnlyList<BulkHireItem> items,
        CancellationToken cancellationToken
    )
    {
        Result<BulkOperationResult> result = await sender.Send(
            new BulkHireEmployeesCommand(items),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Soft-deletes up to 500 employees in one batch — per-item Result reporting.</summary>
    [HttpPost("bulk-delete")]
    [HasPermission(Permissions.Employees.Delete)]
    [ProducesResponseType<BulkOperationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteEmployees(
        [FromBody] IReadOnlyList<string> employeeIds,
        CancellationToken cancellationToken
    )
    {
        Result<BulkOperationResult> result = await sender.Send(
            new BulkDeleteEmployeesCommand(employeeIds),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Ends an employee's employment.</summary>
    [HttpPost("{employeeId}/terminate")]
    [HasPermission(Permissions.Employees.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TerminateEmployee(
        string employeeId,
        [FromBody] TerminateEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new TerminateEmployeeCommand(employeeId, request.TerminatedOn);

        Result result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(NoContent, Problem);
    }
}
