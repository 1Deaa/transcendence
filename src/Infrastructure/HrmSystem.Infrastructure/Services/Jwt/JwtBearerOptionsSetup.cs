using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HrmSystem.Infrastructure.Services.Jwt;

/*
    //?     Configures [JwtBearerOptions] through the ASP.NET Core options pipeline instead of
    //?     an inline lambda in DI. Implements [IConfigureNamedOptions<JwtBearerOptions>] so the
    //?     options system resolves and applies this class automatically when the middleware
    //?     first accesses [JwtBearerOptions] — no manual wiring needed beyond [ConfigureOptions<T>].
    //
    //*     Benefit over the inline-lambda approach:
    //*       - [JwtAuthOptions] arrives via proper DI ([IOptions<T>]) — no raw [IConfiguration] reads.
    //*       - All validation parameters live in one focused class, not scattered in the DI setup.
    //*       - Testable in isolation: construct with a fake [IOptions<JwtAuthOptions>] and assert the output.
    //
    //!     [Configure(string? name, JwtBearerOptions options)] is required by [IConfigureNamedOptions].
    //!     It delegates to [Configure(JwtBearerOptions)] so all logic stays in one place.
    //!     Registered via [services.ConfigureOptions<JwtBearerOptionsSetup>()] — that one call
    //!     hooks up both [IConfigureOptions<JwtBearerOptions>] and [IConfigureNamedOptions<JwtBearerOptions>].
*/
internal sealed class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtAuthOptions _jwtAuthOptions;
    private readonly ILogger<JwtBearerOptionsSetup> _logger;

    public JwtBearerOptionsSetup(IOptions<JwtAuthOptions> options, ILogger<JwtBearerOptionsSetup> logger)
    {
        _jwtAuthOptions = options.Value;
        _logger = logger;
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtAuthOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtAuthOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtAuthOptions.Key)
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            //! Remove this event in production — dev-only to surface the exact validation failure.
            OnAuthenticationFailed = ctx =>
            {
                _logger.LogDebug(
                    ctx.Exception,
                    "[JWT] Authentication failed: {ExceptionType}: {Message}",
                    ctx.Exception.GetType().Name,
                    ctx.Exception.Message
                );
                return Task.CompletedTask;
            },

            /*
                //?     SignalR browsers cannot set an Authorization header on the WebSocket
                //?     handshake — the client library sends the JWT as ?access_token=… instead.
                //!     Scoped to /hubs paths ONLY so tokens never ride query strings on normal
                //!     API calls (they would end up in access logs).
            */
            OnMessageReceived = ctx =>
            {
                string? accessToken = ctx.Request.Query["access_token"];
                if (
                    !string.IsNullOrEmpty(accessToken)
                    && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs")
                )
                {
                    ctx.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }
}
