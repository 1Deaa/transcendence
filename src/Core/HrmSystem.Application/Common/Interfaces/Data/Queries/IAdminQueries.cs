using HrmSystem.Application.Features.Admin.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Queries;

/*
    //?     Dapper read queries behind the admin panel.
    //!     ABSOLUTE RULE (CLAUDE.md): every tenant-scoped method takes an explicit TenantId
    //!     and every SQL statement includes WHERE TenantId = @TenantId — Dapper bypasses
    //!     the EF query filters, so this parameter IS the tenant isolation.
    //!     [GetRolesWithPermissionsAsync] is HOST-LEVEL by design: Identity roles are
    //!     shared platform metadata, not tenant rows.
*/
public interface IAdminQueries
{
    //? Every account of the workspace with its role, activation state, and join date.
    Task<IReadOnlyList<TenantUserSummary>> GetTenantUsersAsync(
        TenantId tenantId,
        CancellationToken ct
    );

    //? Role → permission matrix from the Identity store (host-level metadata).
    Task<IReadOnlyList<RoleSummary>> GetRolesWithPermissionsAsync(CancellationToken ct);
}
