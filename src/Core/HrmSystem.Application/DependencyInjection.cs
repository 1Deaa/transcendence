using FluentValidation;
using HrmSystem.Application.Common.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HrmSystem.Application;

public static class DependencyInjection
{
    /*
        //? Registers all Application-layer services into the DI container.
        //? Called once from the Web layer: builder.Services.AddApplication(builder.Configuration)
     */
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDomainLayerServices().AddMediatRServices(configuration).AddPipelineBehaviors();

        return services;
    }

    public static IServiceCollection AddDomainLayerServices(this IServiceCollection services)
    {
        // You add services that are related to the domain layer here, for example the below; which could be a service that is used by the Handlers to perform some domain logic, do some calculations (which make your Handlers thinner).
        //services.AddTransient<PricingService>();
        return services;
    }

    private static IServiceCollection AddMediatRServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string? licenseKey = configuration["MediatR:LicenseKey"];

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            //! Adding Pipeline Behaviors (decorators) - The Open Generic behaviors
            //! Order matters: ValidationBehavior runs first, then Logging.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));

            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }
        });

        return services;
    }

    private static IServiceCollection AddPipelineBehaviors(this IServiceCollection services)
    {
        /*
            //!     [includeInternalTypes: true] is REQUIRED — every validator in this assembly
            //!     is [internal sealed], and the scan skips non-public types by default.
            //!     Without it not a single validator is registered, [ValidationBehavior] sees
            //!     an empty validator list, and ALL server-side command validation is silently
            //!     skipped (bad emails and weak passwords reach the handlers).
        */
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true
        );
        return services;
    }
}
