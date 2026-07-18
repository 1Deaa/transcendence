using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Admin.Shared;

namespace HrmSystem.Application.Features.Admin.GetRoles;

//? The rules matrix: tenant-assignable roles with their full permission sets.
public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleSummary>>;
