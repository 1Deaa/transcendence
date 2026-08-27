using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Admin.DeleteTenantUser;

public sealed record DeleteTenantUserCommand(string UserId) : ICommand;
