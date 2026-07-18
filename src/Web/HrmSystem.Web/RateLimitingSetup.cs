using System.Globalization;
using System.Threading.RateLimiting;

namespace HrmSystem.Web;

/*
    //?     Rate limiting for the anonymous auth surface (login/refresh/register/...):
    //?     fixed window of 10 requests per minute PER CLIENT IP — generous for humans,
    //?     hostile to credential stuffing. Applied via [EnableRateLimiting("auth")]
    //?     on AuthController only; authenticated API traffic is never limited here.
    //
    //!     Partitioning by RemoteIpAddress means everyone behind one NAT shares a bucket —
    //!     acceptable for Phase 1. Behind the nginx container the API sees the proxy's
    //!     address, so nginx forwards X-Forwarded-For and UseForwardedHeaders (pipeline
    //!     step 2) restores the real client IP before this partitioner runs.
*/
public static class RateLimitingSetup
{
    public const string AuthPolicyName = "auth";

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Window.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            options.AddPolicy(
                AuthPolicyName,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = PermitLimit,
                            Window = Window,
                            QueueLimit = 0,
                        }
                    )
            );
        });

        return services;
    }
}
