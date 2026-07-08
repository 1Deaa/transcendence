using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Users.GetUserById;

/*
    //*     Single handler shared by the API endpoint and the MVC admin page.
    //*     It knows nothing about HTTP, controllers, views, or authentication schemes —
    //*     pure application logic delegating to domain and infrastructure abstractions.
*/
internal sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IIdentityService identityService)
    {
        _userRepository = userRepository;
        _identityService = identityService;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        /*
            //?     Parse early — a malformed ID (missing "user-" prefix, null, whitespace)
            //?     is caught here and returned as a structured [Validation] error before any
            //?     database round-trip is attempted.
            //!     [UserId.From] validates the "user-{UUIDv7}" format; it does NOT hit the DB.
        */
        Result<UserId> userIdResult = UserId.From(query.RawUserId);

        if (userIdResult.IsFailure)
        {
            return userIdResult.Errors;
        }

        UserId userId = userIdResult.Value;

        /*
            //?     [GetByIdAsync] respects global query filters — a soft-deleted or inactive user
            //?     returns [null] here.  Use [FindByIdAsync] if you need to bypass filters
            //?     (e.g. an admin reactivation flow).
            //
            //!     [UserErrors.NotFound] is intentionally opaque: we do not distinguish between
            //!     "user never existed", "soft-deleted", and "deactivated".  Admin tooling that
            //!     needs to surface those states should call a dedicated admin-scoped query.
        */
        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        /*
            //?     Roles and permissions live in the Identity store — [IIdentityService] is the
            //?     only way to reach them from the Application layer without referencing
            //?     [UserManager<AppUser>] (an Infrastructure type).
        */
        IReadOnlyList<string> roles = await _identityService.GetRolesAsync(
            user.IdentityId,
            cancellationToken
        );

        IReadOnlyList<string> permissions = await _identityService.GetPermissionsAsync(
            user.IdentityId,
            cancellationToken
        );

        return new UserProfileResponse(
            DomainUserId: user.Id!.Value,
            IdentityId: user.IdentityId,
            UserName: user.UserName.Value,
            FirstName: user.FirstName.Value,
            MiddleName: user.MiddleName?.Value,
            LastName: user.LastName.Value,
            Email: user.Email.Value,
            PhoneNumber: user.PhoneNumber?.Value,
            IsActive: user.IsActive,
            IsDeleted: user.IsDeleted,
            MemberSince: user.CreatedAt,
            Roles: roles,
            Permissions: permissions
        );
    }
}
