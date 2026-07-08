using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Features.Users.GetCurrentUser;
using HrmSystem.Application.Features.Users.GetUserById;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Web.Api.Controllers.ApiBase;
using HrmSystem.Web.Authentication;

namespace HrmSystem.Web.Api.Controllers.Users;

/*
    //*     User resource endpoints — distinct from [AuthController] (register/login/refresh).
    //*     Authentication is a cross-cutting concern; this controller owns user-data operations.
    //
    //>     Routes owned here:   GET /api/users/me
    //>                          GET /api/users/{userId}
*/
[Route("api/users")]
[ApiController]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController(ISender sender, ICurrentUserContext currentUser)
    : ApiBaseController
{
    /*
        //?     Resolves [DomainUserId] from the bearer token claim here in the controller —
        //?     the presentation layer owns the "who is calling" concern.
        //?     The handler receives a strongly-typed [UserId] and does pure data access.
        //
        //*     [QueryCachingBehavior] intercepts this query automatically because
        //*     [GetCurrentUserQuery] implements [ICachedQuery<CurrentUserResponse>].
        //*     Cache key: "users:me:{userId}" — per-user, 10-minute TTL.
        //
        //!     [Authorize] guarantees a valid principal exists before this action runs.
        //!     The null guard on [DomainUserId] is a safety net for token-claim corruption.
    */
    /// <summary>
    /// Retrieves information about the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// Requires the caller to be authenticated and have permission to read user information. The response is cached per
    /// user for 10 minutes to improve performance. If the authentication token is invalid or corrupted, a problem
    /// response is returned.
    /// </remarks>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An HTTP 200 response containing the current user's details if found; otherwise, an HTTP 404 response if the user
    /// does not exist or is not authenticated.
    /// </returns>
    [HttpGet("me")]
    [Authorize]
    //[HasPermission(Permissions.Users.Read)]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        UserId? domainUserId = currentUser.DomainUserId;

        if (domainUserId is null)
        {
            return Problem([UserErrors.NotAuthenticated]);
        }

        Result<CurrentUserResponse> result = await sender.Send(
            new GetCurrentUserQuery(domainUserId),
            ct
        );

        return result.Match<IActionResult>(Ok, Problem);
    }

    /*
        //?     Route parameter is passed raw to the query.
        //?     The handler validates the "user-{UUIDv7}" format — a malformed segment
        //?     returns 400 ValidationProblem before any DB access.
        //
        //*     [QueryCachingBehavior] caches this query under "users:profile:{userId}" — 10 min TTL.
        //!     In production, restrict this endpoint to an admin-only policy or add a
        //!     caller-ownership check so users can only fetch their own profile.
    */
    /// <summary>
    /// Gets a user by Id (Admin Only)
    /// </summary>
    /// <param name="userId">The User unique identifier</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The User Details</returns>
    [HttpGet("{userId}")]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserById(string userId, CancellationToken ct)
    {
        Result<UserProfileResponse> result = await sender.Send(new GetUserByIdQuery(userId), ct);

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>
    /// Update the user profile
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns>No Content Status code indicates that update is successful</returns>
    [HttpGet("me/profile")]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    //> [EndpointDescription("Update the user profile")] //!Won't Work; it is for Built-in [OpenAPI]; use [XML] Comments instead.
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken ct
    )
    {
        UpdateProfileRequest x = request;
        if (request.Name != null)
        {
            return NoContent(); //! This is best return
        }

        return Ok(new { x, ct });
    }
}
