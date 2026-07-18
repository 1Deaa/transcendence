using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Admin.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Admin.GetTenantUsers;

internal sealed class GetTenantUsersQueryHandler(
    IAdminQueries adminQueries,
    ITenantContext tenantContext
) : IQueryHandler<GetTenantUsersQuery, IReadOnlyList<TenantUserSummary>>
{
    public async Task<Result<IReadOnlyList<TenantUserSummary>>> Handle(
        GetTenantUsersQuery query,
        CancellationToken cancellationToken
    )
    {
        TenantId? tenantId = tenantContext.Current;
        if (tenantId is null)
        {
            return AdminErrors.TenantUnresolved;
        }

        IReadOnlyList<TenantUserSummary> users = await adminQueries.GetTenantUsersAsync(
            tenantId,
            cancellationToken
        );

        return Result.Success(users);
    }
}
