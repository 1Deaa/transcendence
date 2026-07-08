using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Features.Departments.AddDepartment;
using HrmSystem.Application.Features.Departments.GetAllDepartments;
using HrmSystem.Application.Features.Departments.GetDepartmentById;
using HrmSystem.Application.Features.Departments.RemoveDepartment;
using HrmSystem.Application.Features.Departments.Shared;
using HrmSystem.Application.Features.Departments.UpdateDepartment;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Departments;

/*
    //*     Department resource — tenant-scoped CRUD.
    //>     Routes owned here:   GET    /api/departments
    //>                          GET    /api/departments/{departmentId}
    //>                          POST   /api/departments
    //>                          PUT    /api/departments/{departmentId}
    //>                          DELETE /api/departments/{departmentId}
*/
[Route("api/departments")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DepartmentsController(ISender sender) : ApiBaseController
{
    /// <summary>Lists all departments of the caller's tenant.</summary>
    [HttpGet]
    [HasPermission(Permissions.Departments.Read)]
    [ProducesResponseType<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDepartments(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DepartmentResponse>> result = await sender.Send(
            new GetAllDepartmentsQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Gets a single department by id.</summary>
    [HttpGet("{departmentId}")]
    [HasPermission(Permissions.Departments.Read)]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentById(
        string departmentId,
        CancellationToken cancellationToken
    )
    {
        Result<DepartmentResponse> result = await sender.Send(
            new GetDepartmentByIdQuery(departmentId),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Creates a new department.</summary>
    [HttpPost]
    [HasPermission(Permissions.Departments.Write)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddDepartment(
        [FromBody] AddDepartmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new AddDepartmentCommand(request.Name, request.Code);

        Result<string> result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            departmentId =>
                CreatedAtAction(nameof(GetDepartmentById), new { departmentId }, departmentId),
            Problem
        );
    }

    /// <summary>Updates a department's name and code.</summary>
    [HttpPut("{departmentId}")]
    [HasPermission(Permissions.Departments.Modify)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDepartment(
        string departmentId,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateDepartmentCommand(departmentId, request.Name, request.Code);

        Result result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Soft-deletes a department (blocked while employees are assigned).</summary>
    [HttpDelete("{departmentId}")]
    [HasPermission(Permissions.Departments.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveDepartment(
        string departmentId,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new RemoveDepartmentCommand(departmentId),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }
}
