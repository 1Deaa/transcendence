using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using HrmSystem.Infrastructure.Persistence.Seeding;
using HrmSystem.Web.Hubs;
using HrmSystem.Web.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace HrmSystem.Web;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        //> Migrate both DBs (Identity/App) and run all seeders (config-only in Production, full in Development).
        await app.InitialiseDatabaseAsync();

        //! 1. Exception handling should be FIRST to catch all errors
        /*
                //* - AddProblemDetails don't show errors using RFC 9457 Problem Details format in {{Production Environment}}
                //* - Handle Exception in (Not Development Environment) as {Production Environment} to produce a standardize API error responses using the RFC 9457 Problem Details format
                //app.UseExceptionHandler("/Home/Error");
        */
        app.UseExceptionHandler();

        //! 2. Status code pages for handling HTTP status codes
        app.UseStatusCodePages();

        /*
                //if (app.Environment.IsDevelopment())
                //{
                //    // Expose the raw OpenAPI JSON at /openapi/v1.json in dev.
                //}
                //else
                //{

                //}
         */

        app.MapOpenApiServices();

        //! Skip HTTPS enforcement inside Docker — the container speaks plain HTTP on 8080.
        //! HTTPS termination belongs at the reverse proxy (Nginx / Traefik) in staging/prod.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        //! 3. Serilog request logging (early to log all requests)
        //app.UseSerilogRequestLogging();

        //! 4. Static Files: Serves static files (JS, CSS, images) and short-circuits.
        //* Placed early to avoid unnecessary processing for static content.
        app.UseStaticFiles();

        //! 5. Routing: Determines which endpoint to execute.
        app.UseRouting();

        //! 6. CORS: Must come after UseRouting and before UseAuthentication and UseAuthorization.
        //* This allows policy evaluation based on the selected endpoint's metadata.
        //app.UseCors(configuration["AppSettings:CorsPolicyName"]!);

        //! 7. Rate limiting (before authentication to protect auth endpoints)
        //app.UseRateLimiter();

        //! 8. Authentication: Identifies the user. Who are you?
        app.UseAuthentication();

        //! 9. Authorization: Checks if the user has permission. Are you allowed?
        app.UseAuthorization();

        //! 10. Output caching (after auth to cache based on user context)
        //app.UseOutputCache();

        //! 11. Endpoint Mapping.
        /*
            //* MapControllers picks up attribute-routed controllers (the API family) Like: ```[Route("habits")]``` with    [ApiController] by default.
            //* The two MapControllerRoute calls handle MVC's conventional routing:
            //*  admin area first (more specific), then the public site default.
         */
        app.MapControllers();

        /*
            //?     No MVC conventional routes here — this host is API-only.
            //?     All UI (dashboards, status page, admin) lives in the Blazor WASM client.
            //>     The reference project mapped an admin Area + default MVC route at this point.
        */

        /*
            //?     Raw health endpoints (tag-filtered), JSON-shaped by the HealthChecks.UI writer:
            //!      /health       → liveness  ("self" only) — Docker/orchestrator probe.
            //!      /health/ready → readiness (sqlserver, redis, scheduler) — 503 when unhealthy.
            //>     Human/status-page consumers use /api/status instead (always 200, cached, redacted).
        */
        app.MapHealthChecks(
            "/health",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            }
        );

        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            }
        );

        //? Status-page push channel — anonymous, server→client only.
        app.MapHub<StatusHub>("/hubs/status");

        //? Live-dashboard push channel — JWT via ?access_token, per-tenant groups.
        app.MapHub<AnalyticsHub>("/hubs/analytics");

        //? CSV-import progress channel — JWT via ?access_token, per-tenant groups.
        app.MapHub<ImportProgressHub>("/hubs/imports");

        //! 12. Antiforgery: Protects against CSRF attacks. Must be last in the pipeline.
        //app.UseAntiforgery();

        //app.MapStaticAssets();
        //app.MapHub<WorkOrderHub>("/hubs/workorders"); //! Example of SignalR configuration.

        return app;
    }

    /*
        //*     Single entry point for all database startup work — migrations + seeding.
        //!     Why CreateScope()? At startup there is no active HTTP scope.
        //!     DbContext is registered as Scoped, so resolving it from the singleton root
        //!     IServiceProvider throws "Cannot consume scoped service from singleton".
        //?     CreateScope() creates a temporary request-like lifetime: the scoped DbContext is
        //?     resolved, used, and disposed cleanly when the using block ends.
    */
    private static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        DatabaseInitialiser initialiser =
            scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }

    private static WebApplication MapOpenApiServices(this WebApplication app)
    {
        //! THIS IS [BUILT-IN] OpenApi Library --> it has a limitations in documenting our Apis so we want to replace it with the swagger generator for the OpenApi Specification.
        //app.MapOpenApi(); //! Bad Built-in OpenApi

        //! Good Swagger generator for OpenApi Specification (So we commented the `app.MapOpenApi()` and replaced it with `app.UseSwagger()` and `app.UseSwaggerUI()`)
        app.UseSwagger();
        app.UseSwaggerUI();

        //> No need to config options it will do it automatically
        //app.UseSwaggerUI(swaggerUIOptions =>
        //{
        //    swaggerUIOptions.SwaggerEndpoint("/swagger/v1/swagger.json", "HrmSystem API V1");
        //});

        ConfigureScalarOptions scalarOptions = new(
            app.Services.GetRequiredService<IApiVersionDescriptionProvider>(),
            app.Configuration
        );
        app.MapScalarApiReference(scalarOptions.Configure);

        return app;
    }
}
