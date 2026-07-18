using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Tenants.RegisterCompany;

/*
    //?     Orchestration:
    //?       1. Validate BOTH aggregates first (Tenant + admin User) so the caller gets
    //?          every broken rule in one response, not one aggregate at a time.
    //?       2. Stage the Tenant in the application context, link the User to it, then let
    //?          [IIdentityService.RegisterAsync] commit everything in its shared
    //?          cross-context transaction — tenant, domain user, and Identity user land
    //?          atomically or not at all.
    //?       3. Grant the TenantAdmin role and issue tokens that already carry the
    //?          role + permission + tenant_id claims — the founder is productive immediately.
*/
internal sealed class RegisterCompanyCommandHandler(
    ITenantRepository tenantRepository,
    IIdentityService identityService,
    IJwtTokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<RegisterCompanyCommand, AccessTokensResponse>
{
    public async Task<Result<AccessTokensResponse>> Handle(
        RegisterCompanyCommand command,
        CancellationToken cancellationToken
    )
    {
        var errors = new List<Error>();

        Result<Tenant> tenantResult = Tenant.Create(command.CompanyName, command.Slug);
        if (tenantResult.IsFailure)
        {
            errors.AddRange(tenantResult.Errors);
        }

        Result<User> userResult = User.Create(
            userName: command.UserName,
            firstName: command.FirstName,
            middleName: null,
            lastName: command.LastName,
            email: command.Email
        );
        if (userResult.IsFailure)
        {
            errors.AddRange(userResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Tenant tenant = tenantResult.Value;

        Tenant? existing = await tenantRepository.FindBySlugAsync(
            tenant.Slug.Value,
            cancellationToken
        );
        if (existing is not null)
        {
            return TenantErrors.DuplicateSlug(tenant.Slug.Value);
        }

        /*
            //!     Order matters: the Tenant is STAGED (tracked, not saved) before
            //!     [RegisterAsync] runs — its [SaveChangesAsync] inside the shared
            //!     transaction persists tenant + domain user + Identity user together.
        */
        await tenantRepository.AddAsync(tenant, cancellationToken);

        User adminUser = userResult.Value;
        adminUser.AssignToTenant(tenant.Id!);

        Result<UserId> registerResult = await identityService.RegisterAsync(
            adminUser,
            command.Password,
            cancellationToken
        );
        if (registerResult.IsFailure)
        {
            return registerResult.Errors;
        }

        Result roleResult = await identityService.AddToRoleAsync(
            adminUser.IdentityId,
            RoleNames.TenantAdmin,
            cancellationToken
        );
        if (roleResult.IsFailure)
        {
            return roleResult.Errors;
        }

        DateTimeOffset refreshExpiry = DateTimeOffset.UtcNow.Add(
            tokenProvider.RefreshTokenLifetime
        );
        Result<RefreshToken> refreshTokenResult = RefreshToken.Create(
            adminUser.Id!,
            refreshExpiry
        );
        if (refreshTokenResult.IsFailure)
        {
            return refreshTokenResult.Errors;
        }

        await refreshTokenRepository.AddAsync(refreshTokenResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyList<string> permissions = await identityService.GetPermissionsAsync(
            adminUser.IdentityId,
            cancellationToken
        );

        (string accessToken, DateTime expiresOnUtc) = tokenProvider.CreateAccessToken(
            new GenerateTokenRequest(
                IdentityUserId: adminUser.IdentityId,
                DomainUserId: adminUser.Id!.Value,
                Email: adminUser.Email.Value,
                UserName: adminUser.UserName.Value,
                PhoneNumber: adminUser.PhoneNumber?.Value,
                Roles: [RoleNames.TenantAdmin],
                Permissions: permissions,
                TenantId: tenant.Id!.Value
            )
        );

        return new AccessTokensResponse(
            accessToken,
            refreshTokenResult.Value.Token.Value,
            expiresOnUtc
        );
    }
}
