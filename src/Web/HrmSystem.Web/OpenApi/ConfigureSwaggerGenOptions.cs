using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HrmSystem.Web.OpenApi;

public sealed class ConfigureSwaggerGenOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider
) : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _apiVersionDescriptionProvider =
        apiVersionDescriptionProvider;

    public void Configure(string? name, SwaggerGenOptions options)
    {
        Configure(options); //! Call the unnamed `Configure(...)`
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (
            ApiVersionDescription apiVersionDescription in _apiVersionDescriptionProvider.ApiVersionDescriptions
        )
        {
            options.SwaggerDoc(
                apiVersionDescription.GroupName,
                CreateVersionInfo(apiVersionDescription)
            );
        }

        options.ResolveConflictingActions(apiDescriptions =>
        {
            return apiDescriptions.First();
        });

        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        options.IncludeXmlComments(xmlPath);

        //! This will help in fix some issues that run if I have a `Generic <T>` Types inside the code!
        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

        //! This will make the [Query Parameters] described better in the OpenApi Specification
        options.DescribeAllParametersInCamelCase();

        /*
            //?     [SecuritySchemeType.Http] + [Scheme = "bearer"] — Swagger UI prepends "Bearer "
            //?     automatically so you paste only the raw token in the Authorize dialog.
            //
            //*     Official Swashbuckle 10.x / Microsoft.OpenApi 2.0 pattern:
            //*     Pass the [document] into [OpenApiSecuritySchemeReference] so the reference
            //*     is resolved against the actual document — without it the link is dangling
            //*     and Swagger UI won't inject the Authorization header into requests.
        */
        options.AddSecurityDefinition(
            "bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "JWT Authorization header using the Bearer scheme. Paste only the raw token — Swagger prepends 'Bearer ' automatically.",
            }
        );

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = [],
        });
    }

    private static OpenApiInfo CreateVersionInfo(ApiVersionDescription apiVersionDescription)
    {
        var openApiInfo = new OpenApiInfo
        {
            Title = $"{nameof(HrmSystem)}.API v{apiVersionDescription.ApiVersion}",
            Version = apiVersionDescription.ApiVersion.ToString(),
        };

        if (apiVersionDescription.IsDeprecated)
        {
            openApiInfo.Description = string.Concat(
                openApiInfo.Description,
                " - ",
                "This API Version has been deprecated"
            );
        }

        return openApiInfo;
    }
}
