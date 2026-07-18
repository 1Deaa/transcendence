using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Admin.ChangeUserRole;

//? Replaces the target user's role with exactly one tenant role.
public sealed record ChangeUserRoleCommand(string UserId, string Role) : ICommand;
