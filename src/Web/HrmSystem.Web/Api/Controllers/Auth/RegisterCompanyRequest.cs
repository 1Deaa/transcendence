using HrmSystem.Application.Features.Tenants.RegisterCompany;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record RegisterCompanyRequest(
    string CompanyName,
    string Slug,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
)
{
    public RegisterCompanyCommand ToCommand() =>
        new(CompanyName, Slug, FirstName, LastName, UserName, Email, Password, ConfirmPassword);
}
