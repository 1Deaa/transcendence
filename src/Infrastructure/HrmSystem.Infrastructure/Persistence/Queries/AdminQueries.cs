using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Features.Admin.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Infrastructure.Persistence.Queries;

/*
    //*     Handles ONLY admin panel reads using Dapper — joins the domain Users table
    //*     with the Identity role tables in one round trip.
    //!     ABSOLUTE RULE: every tenant-scoped statement filters WHERE TenantId = @TenantId —
    //!     Dapper bypasses the EF query filters, so this clause IS the tenant isolation.
    //!     [GetRolesWithPermissionsAsync] reads host-level Identity metadata — no tenant filter.
*/
public class AdminQueries(ISqlConnectionFactory connectionFactory) : IAdminQueries
{
    //* Shape of one flat row before grouping — a user may join multiple role rows.
    private sealed record TenantUserRow(
        string UserId,
        string UserName,
        string FirstName,
        string LastName,
        string Email,
        string? Role,
        bool IsActive,
        DateTime CreatedAt
    );

    public async Task<IReadOnlyList<TenantUserSummary>> GetTenantUsersAsync(
        TenantId tenantId,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT u.Id        AS UserId,
                   u.UserName,
                   u.FirstName,
                   u.LastName,
                   u.Email,
                   r.Name      AS Role,
                   u.IsActive,
                   u.CreatedAt
            FROM [HrmSystem].[Users] u
            LEFT JOIN [Identity].[UserRoles] ur ON ur.UserId = u.IdentityId
            LEFT JOIN [Identity].[Roles] r ON r.Id = ur.RoleId
            WHERE u.TenantId = @TenantId AND u.IsDeleted = 0
            ORDER BY u.FirstName, u.LastName
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value },
            cancellationToken: ct
        );

        IEnumerable<TenantUserRow> rows = await connection.QueryAsync<TenantUserRow>(command);

        /*
            //?     One row per user: the RBAC model keeps a single tenant role per account,
            //?     but a LEFT JOIN can still fan out — group and take the first role.
        */
        return rows.GroupBy(row => row.UserId)
            .Select(group =>
            {
                TenantUserRow first = group.First();
                return new TenantUserSummary(
                    first.UserId,
                    first.UserName,
                    first.FirstName,
                    first.LastName,
                    first.Email,
                    group.Select(r => r.Role).FirstOrDefault(role => role is not null) ?? "None",
                    first.IsActive,
                    first.CreatedAt
                );
            })
            .OrderBy(user => user.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.LastName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<RoleSummary>> GetRolesWithPermissionsAsync(
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT r.Name AS RoleName, rc.ClaimValue AS Permission
            FROM [Identity].[Roles] r
            LEFT JOIN [Identity].[RoleClaims] rc
                   ON rc.RoleId = r.Id AND rc.ClaimType = 'permission'
            ORDER BY r.Name, rc.ClaimValue
            """;

        var command = new CommandDefinition(commandText: sql, cancellationToken: ct);

        IEnumerable<(string RoleName, string? Permission)> rows = await connection.QueryAsync<(
            string,
            string?
        )>(command);

        return rows.GroupBy(row => row.RoleName)
            .Select(group => new RoleSummary(
                group.Key,
                group
                    .Where(r => r.Permission is not null)
                    .Select(r => r.Permission!)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList()
            ))
            .ToList();
    }
}
