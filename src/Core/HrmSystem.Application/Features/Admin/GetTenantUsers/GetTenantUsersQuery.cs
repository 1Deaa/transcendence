using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Admin.Shared;

namespace HrmSystem.Application.Features.Admin.GetTenantUsers;

//? The admin panel's user table — every account of the caller's workspace.
public sealed record GetTenantUsersQuery : IQuery<IReadOnlyList<TenantUserSummary>>;
