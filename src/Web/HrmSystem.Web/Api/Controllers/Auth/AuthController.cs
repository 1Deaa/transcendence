using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Web.Api.Controllers.ApiBase;

namespace HrmSystem.Web.Api.Controllers.Auth;

/*
    //*     Keeping [AuthController] separate from [UsersController] is a deliberate architectural decision.
    //*     Authentication is a cross-cutting concern — register, login, refresh, logout — it does not
    //*     belong alongside CRUD operations on user resources (/api/users/{id}, pagination, etc.).
    //*     Mixing them would violate Single Responsibility and make the route surface confusing.
    //
    //>     Routes owned here:   POST /api/auth/register
    //>                          POST /api/auth/login
    //>                          POST /api/auth/refresh
    //>                          POST /api/auth/forgot-password
    //>                          POST /api/auth/reset-password
    //>                          GET  /api/auth/confirm-email
*/
[Route("api/auth")]
[ApiController]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AuthController : ApiBaseController
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken ct
    )
    {
        //!! OLD Implementation - (Bad I mix validation concerns in Controller; while the Controller is for Dispatching; not validating!.
        /*
            //!     [ConfirmPassword] is a Web-layer concern — checked here before the command is built.
            //!     The domain and application layers never see it.
            //?     Using [ModelState] produces a standard RFC 9457 ValidationProblem response,
            //?     consistent with how the FluentValidation pipeline behavior reports command errors.
            ////             if (request.Password != request.ConfirmPassword)
            ////             {
            ////                 ModelState.AddModelError(
            ////                     nameof(request.ConfirmPassword),
            ////                     "Password and confirmation password do not match."
            ////                 );
            ////                 return ValidationProblem(ModelState);
            ////             }
        */

        /*
            //* ----- Description of everything will happen by this Endpoint
            //?     What happens under the hood when [_sender.Send] is called:
            //?       1. FluentValidation pipeline behavior runs [RegisterUserCommandValidator]
            //?       2. [RegisterUserCommandHandler] creates the domain [User] (pure domain validation)
            //?       3. [IIdentityService.RegisterAsync] is called with the valid domain [User]:
            //
            //!       - Problem we solved: two DbContexts ([ApplicationDbContext] + [ApplicationIdentityDbContext])
            //!          each open their own connection and run their own transaction by default. Without
            //!          coordination, Identity user creation and domain User persistence are separate
            //!          transactions — if one succeeds and the other fails, the database is left in an
            //!          inconsistent state (orphaned Identity user with no domain User, or vice versa).
            //
            //>         Solution (inside [IIdentityService] implementation in Infrastructure):
            //>           1. Begin a transaction on [ApplicationIdentityDbContext] (the "owner" context).
            //>           2. Hand its [DbConnection] to [ApplicationDbContext] via SetDbConnection.
            //>           3. Enlist [ApplicationDbContext] in the same transaction via UseTransactionAsync.
            //>               → Both contexts now write through the same connection/transaction.
            //>           4. Create ASP.NET Identity user via [UserManager<AppUser>] => INSERT fires here
            //>           5. Link the identity ID onto the domain [User] via [user.SetIdentityId(...)].
            //>           6. Persist the domain [User] via [ApplicationDbContext.SaveChangesAsync].
            //>           7. [identityTransaction.CommitAsync()] — ONE commit persists everything atomically.
            //>           8. Handler generates tokens via [IJwtTokenProvider] — auto-login on registration.
            //
            //!       - If [CommitAsync] is never reached (exception, Identity failure, domain failure)
            //!          the transaction goes out of scope and [SQL Server] rolls back ALL changes automatically.
            //!          No orphaned data. No partial writes.
        */
        Result<AccessTokensResponse> result = await _sender.Send(request.ToCommand(), ct);

        return result.Match<IActionResult>(tokens => Ok(tokens), Problem);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request,
        CancellationToken ct
    )
    {
        Result<AccessTokensResponse> result = await _sender.Send(request.ToCommand(), ct);

        return result.Match<IActionResult>(Ok, Problem);
    }

    /*
        //!     Returns 200 OK regardless of whether the email is registered — never reveals
        //!     whether the address belongs to a user (enumeration protection).
        //?     If the email is registered, a password-reset link is sent to it.
    */
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct
    )
    {
        Result result = await _sender.Send(request.ToCommand(), ct);

        return result.Match<IActionResult>(() => Ok(), Problem);
    }

    /*
        //?     Consumes the reset token from the email link and sets a new password.
        //!     Token is single-use — a second attempt with the same token returns [InvalidToken].
    */
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct
    )
    {
        Result result = await _sender.Send(request.ToCommand(), ct);

        return result.Match<IActionResult>(() => Ok(), Problem);
    }

    /*
        //?     Confirms the user's email address using the link sent after registration.
        //>     Confirmation link format: GET /api/auth/confirm-email?userId=...&token=...
    */
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string token,
        CancellationToken ct
    )
    {
        Result result = await _sender.Send(new ConfirmEmailRequest(userId, token).ToCommand(), ct);

        return result.Match<IActionResult>(() => Ok(), Problem);
    }

    /*
        //?     Exchanges a valid refresh token for a new access token + rotated refresh token.
        //?     The old refresh token value is invalidated on the server the moment [Rotate] is
        //?     persisted — replay attacks using the old value will return [RefreshToken.Invalid].
        //
        //*     Rotation strategy: update the existing [RefreshToken] row in-place.
        //*       - Audit trail (CreatedAt) is preserved.
        //*       - No table growth from accumulating rows on every refresh.
        //*       - One row per active session per user.
        //
        //!     The client MUST replace both tokens immediately — the old refresh token is
        //!     dead the moment this endpoint responds successfully.
    */
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct
    )
    {
        Result<AccessTokensResponse> result = await _sender.Send(request.ToCommand(), ct);

        return result.Match<IActionResult>(Ok, Problem);
    }
}
