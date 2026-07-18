using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Tenants.RegisterCompany;

/*
    //?     Public self-service signup — creates a company workspace AND its first
    //?     TenantAdmin account in one atomic operation, then auto-logs the admin in.
    //>     Driven by the landing page "Start your workspace" form.
*/
public sealed record RegisterCompanyCommand(
    string CompanyName,
    string Slug,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
) : ICommand<AccessTokensResponse>;
