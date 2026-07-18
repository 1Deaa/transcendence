using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Admin.Shared;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Admin.GetRoles;

internal sealed class GetRolesQueryHandler(IAdminQueries adminQueries)
    : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleSummary>>
{
    public async Task<Result<IReadOnlyList<RoleSummary>>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<RoleSummary> allRoles = await adminQueries.GetRolesWithPermissionsAsync(
            cancellationToken
        );

        //! HostAdmin never leaves the platform level — tenant admins cannot see or assign it.
        IReadOnlyList<RoleSummary> assignable = allRoles
            .Where(role => RoleNames.TenantAssignable.Contains(role.Name))
            .ToList();

        return Result.Success(assignable);
    }
}
