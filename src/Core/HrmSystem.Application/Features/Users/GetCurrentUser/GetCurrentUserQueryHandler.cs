using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Users.GetCurrentUser;

/*
    //*     Pure application-layer handler: given a [UserId], load and project the user profile.
    //*     No HTTP context, no authentication scheme, no claim parsing — those are all
    //*     resolved by the caller (controller) before the query is dispatched.
    //
    //!     [ICurrentUserContext] was intentionally removed from this handler.
    //!     Authentication is a presentation-layer concern; by the time the query reaches here,
    //!     [DomainUserId] has already been resolved and validated by the controller.
    //!     [Authorize] on the endpoint enforces that — the handler trusts its input.
*/
internal sealed class GetCurrentUserQueryHandler
    : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IIdentityService identityService
    )
    {
        _userRepository = userRepository;
        _identityService = identityService;
    }

    public async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken
    )
    {
        UserId userId = query.DomainUserId;

        /*
            //?     [GetByIdAsync] applies global query filters — soft-deleted or inactive user
            //?     returns [null].  We do not distinguish those states here.
        */
        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        IReadOnlyList<string> roles = await _identityService.GetRolesAsync(
            user.IdentityId,
            cancellationToken
        );

        IReadOnlyList<string> permissions = await _identityService.GetPermissionsAsync(
            user.IdentityId,
            cancellationToken
        );

        return new CurrentUserResponse(
            DomainUserId: user.Id!.Value,
            IdentityId: user.IdentityId,
            UserName: user.UserName.Value,
            FirstName: user.FirstName.Value,
            MiddleName: user.MiddleName?.Value,
            LastName: user.LastName.Value,
            Email: user.Email.Value,
            PhoneNumber: user.PhoneNumber?.Value,
            SecondaryEmail: user.SecondaryEmail?.Value,
            SecondaryPhoneNumber: user.SecondaryPhoneNumber?.Value,
            IsActive: user.IsActive,
            MemberSince: user.CreatedAt,
            Roles: roles,
            Permissions: permissions
        );
    }
}
