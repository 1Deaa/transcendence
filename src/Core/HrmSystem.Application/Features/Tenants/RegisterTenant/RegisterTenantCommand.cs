using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Tenants.RegisterTenant;

public sealed record RegisterTenantCommand(string CompanyName, string Slug) : ICommand<string>;
