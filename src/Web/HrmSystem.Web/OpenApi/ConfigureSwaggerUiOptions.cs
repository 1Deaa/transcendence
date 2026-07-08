using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace HrmSystem.Web.OpenApi;

public sealed class ConfigureSwaggerUiOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider
) : IConfigureNamedOptions<SwaggerUIOptions>
{
    private readonly IApiVersionDescriptionProvider _apiVersionDescriptionProvider =
        apiVersionDescriptionProvider;

    public void Configure(string? name, SwaggerUIOptions options)
    {
        Configure(options);
    }

    public void Configure(SwaggerUIOptions options)
    {
        foreach (
            ApiVersionDescription apiVersionDescription in _apiVersionDescriptionProvider.ApiVersionDescriptions
        )
        {
            options.SwaggerEndpoint(
                $"/swagger/{apiVersionDescription.GroupName}/swagger.json",
                apiVersionDescription.GroupName.ToUpperInvariant()
            );
        }
    }
}
