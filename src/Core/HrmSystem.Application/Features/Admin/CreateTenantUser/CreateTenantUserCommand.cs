using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Admin.CreateTenantUser;

/*
    //?     Admin-panel account provisioning — creates a login for a person of the
    //?     caller's OWN workspace with exactly one tenant role.
    //>     Returns the new domain UserId.
*/
public sealed record CreateTenantUserCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role
) : ICommand<string>;
