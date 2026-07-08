using Asp.Versioning.ApiExplorer;
using Scalar.AspNetCore;

namespace HrmSystem.Web.OpenApi;

/*
    //?     Mirrors the shape of [ConfigureSwaggerUiOptions]: a single class that owns
    //?     all Scalar-specific wiring so [WebApplicationExtensions] stays thin.
    //
    //!     Scalar's [MapScalarApiReference] does not integrate with [IConfigureOptions<T>],
    //!     so this class cannot be registered via [services.ConfigureOptions<T>()].
    //!     Instead, resolve it from the DI container and pass [Configure] as the delegate.
    //
    //*     [WithOpenApiRoutePattern] — path to the spec JSON (what to document).
    //*     [AddServer]              — host where the real API lives (where to send requests).
    //*     These are two different concerns: spec location vs. API target.
*/
internal sealed class ConfigureScalarOptions(
    IApiVersionDescriptionProvider versionProvider,
    IConfiguration configuration
)
{
    public void Configure(ScalarOptions options)
    {
        string apiBaseUrl =
            configuration["ApiBaseUrl"]
            ?? throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.");

        options
            .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
            .AddDocuments(versionProvider.ApiVersionDescriptions.Select(d => d.GroupName))
            .AddServer(apiBaseUrl, "Local");
    }
}
