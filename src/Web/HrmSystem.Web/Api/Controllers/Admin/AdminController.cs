using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Features.Admin.ChangeUserRole;
using HrmSystem.Application.Features.Admin.CreateTenantUser;
using HrmSystem.Application.Features.Admin.GetRoles;
using HrmSystem.Application.Features.Admin.GetTenantUsers;
using HrmSystem.Application.Features.Admin.SetUserActivation;
using HrmSystem.Application.Features.Admin.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Admin;

/*
    //*     Workspace administration — accounts, roles, and the permission matrix.
    //>     Routes owned here:   GET  /api/admin/users                    (user table)
    //>                          POST /api/admin/users                    (provision account)
    //>                          PUT  /api/admin/users/{id}/role          (change role)
    //>                          PUT  /api/admin/users/{id}/activation    (suspend/reinstate)
    //>                          GET  /api/admin/roles                    (roles + permissions)
    //
    //!     Every tenant-scoped operation resolves the workspace from the caller's ambient
    //!     tenant — a TenantAdmin can never reach another company's accounts.
*/
[Route("api/admin")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminController(ISender sender) : ApiBaseController
{
    /// <summary>Lists every account of the caller's workspace with role and status.</summary>
    [HttpGet("users")]
    [HasPermission(Permissions.Admin.ManageUsers)]
    [ProducesResponseType<IReadOnlyList<TenantUserSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<TenantUserSummary>> result = await sender.Send(
            new GetTenantUsersQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Provisions a new workspace account with one tenant role.</summary>
    [HttpPost("users")]
    [HasPermission(Permissions.Admin.ManageUsers)]
    [ProducesResponseType<string>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateTenantUserRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<string> result = await sender.Send(
            new CreateTenantUserCommand(
                request.UserName,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.Role
            ),
            cancellationToken
        );

        return result.Match<IActionResult>(
            userId => CreatedAtAction(nameof(GetUsers), routeValues: null, userId),
            Problem
        );
    }

    /// <summary>Replaces the user's role with the given tenant role.</summary>
    [HttpPut("users/{userId}/role")]
    [HasPermission(Permissions.Admin.ManageRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserRole(
        [FromRoute] string userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new ChangeUserRoleCommand(userId, request.Role),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Suspends or reinstates a workspace account.</summary>
    [HttpPut("users/{userId}/activation")]
    [HasPermission(Permissions.Admin.ManageUsers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserActivation(
        [FromRoute] string userId,
        [FromBody] SetUserActivationRequest request,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new SetUserActivationCommand(userId, request.IsActive == true),
            cancellationToken
        );

        return result.Match<IActionResult>(NoContent, Problem);
    }

    /// <summary>Lists the tenant-assignable roles with their full permission sets.</summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.Admin.Read)]
    [ProducesResponseType<IReadOnlyList<RoleSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RoleSummary>> result = await sender.Send(
            new GetRolesQuery(),
            cancellationToken
        );

        return result.Match<IActionResult>(Ok, Problem);
    }
}
