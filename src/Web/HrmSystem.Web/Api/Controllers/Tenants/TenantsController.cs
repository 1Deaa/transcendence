using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Features.Tenants.RegisterTenant;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HrmSystem.Web.Api.Controllers.Tenants;

/*
    //*     Host-level tenant administration — platform staff only.
    //>     Routes owned here:   POST /api/tenants
*/
[Route("api/tenants")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class TenantsController(ISender sender) : ApiBaseController
{
    /// <summary>Registers a new company workspace (tenant).</summary>
    [HttpPost]
    [HasPermission(Permissions.Tenants.Manage)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterTenant(
        [FromBody] RegisterTenantRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new RegisterTenantCommand(request.CompanyName, request.Slug);

        Result<string> result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            tenantId => CreatedAtAction(nameof(RegisterTenant), new { id = tenantId }, tenantId),
            Problem
        );
    }
}
