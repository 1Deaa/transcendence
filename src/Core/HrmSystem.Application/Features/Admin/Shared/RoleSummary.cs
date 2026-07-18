namespace HrmSystem.Application.Features.Admin.Shared;

//? One assignable role with its full permission set — the admin panel's rules matrix.
public sealed record RoleSummary(string Name, IReadOnlyList<string> Permissions);
