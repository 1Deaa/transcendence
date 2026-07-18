namespace HrmSystem.Web.Api.Controllers.Admin;

public sealed record CreateTenantUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role
);
