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
