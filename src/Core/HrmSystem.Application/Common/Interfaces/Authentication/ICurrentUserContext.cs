using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Authentication;

/*
    //?     Abstracts the current authenticated user's identity so Application-layer handlers
    //?     can read claims without depending on [HttpContext] or ASP.NET Core types.
    //
    //*     Works for both authentication schemes because both stamp the same claim names:
    //*       - JWT Bearer (API)  → claims written by [JwtTokenProvider.CreateAccessToken]
    //*       - MVC Cookie        → claims written by [MvcLoginCommandHandler] / [MvcRegisterCommandHandler]
    //
    //!     Only read claims that were written at authentication time.
    //!     Do NOT add [Roles] or [Permissions] here — those require a DB call and belong
    //!     in [IIdentityService]. This interface is a zero-cost claim reader.
*/
public interface ICurrentUserContext
{
    //*     True when the request carries a valid, authenticated principal.
    bool IsAuthenticated { get; }

    /*
        //?     Domain [UserId] parsed from the [domain_sub] claim.
        //!     Returns [null] when the claim is absent or contains a corrupted value.
        //!     This is the primary key for loading the domain [User] entity.
    */
    UserId? DomainUserId { get; }

    //?     ASP.NET Identity PK — from the standard [sub] claim.
    string? IdentityId { get; }

    //?     Primary email — from the [email] claim.
    string? Email { get; }

    //?     Display / login username — from the [preferred_username] claim.
    string? UserName { get; }

    //?     Phone number — from the [phone_number] claim; null when not set.
    string? PhoneNumber { get; }
}
