using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HrmSystem.Infrastructure.HealthChecks;

/*
    //?     Watches the process working set — a slow leak shows up here long before the
    //?     container OOM-kills the API. Degraded > MaxWorkingSetMb; Unhealthy > 1.5×.
*/
internal sealed class MemoryHealthCheck(IOptions<HealthOptions> options) : IHealthCheck
{
    private readonly HealthOptions _options = options.Value;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        double workingSetMb = Process.GetCurrentProcess().WorkingSet64 / 1_048_576d;

        string description =
            $"Working set {workingSetMb:F0} MB (threshold {_options.MaxWorkingSetMb} MB).";

        HealthCheckResult result = workingSetMb switch
        {
            _ when workingSetMb > _options.MaxWorkingSetMb * 1.5
                => HealthCheckResult.Unhealthy(description),
            _ when workingSetMb > _options.MaxWorkingSetMb
                => HealthCheckResult.Degraded(description),
            _ => HealthCheckResult.Healthy(description),
        };

        return Task.FromResult(result);
    }
}
