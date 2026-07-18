using HrmSystem.Application;
using HrmSystem.Infrastructure;
using HrmSystem.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//? Layer-by-layer service registration — one extension method per Clean Architecture layer.
builder
    .Services.AddPresentation(builder)
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

await app.ConfigurePipelineAsync();

await app.RunAsync();

/*
    //?     Top-level statements compile into an implicit internal [Program] class.
    //?     Web.IntegrationTests boots the real host via WebApplicationFactory<Program> —
    //?     public so the (public, xUnit-discovered) fixture/test classes can reference it.
*/
public partial class Program;
