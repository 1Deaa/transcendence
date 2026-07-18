namespace HrmSystem.Application.Features.Admin.Shared;

//? One row of the admin panel's user table — account + role + activation state.
public sealed record TenantUserSummary(
    string UserId,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);
