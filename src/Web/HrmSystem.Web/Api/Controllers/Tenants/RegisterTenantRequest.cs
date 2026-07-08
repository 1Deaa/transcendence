namespace HrmSystem.Web.Api.Controllers.Tenants;

//? API request DTO — kept separate from the command so the wire contract can evolve independently.
public sealed record RegisterTenantRequest(string CompanyName, string Slug);
