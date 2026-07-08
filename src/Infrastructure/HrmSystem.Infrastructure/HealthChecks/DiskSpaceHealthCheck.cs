using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HrmSystem.Infrastructure.HealthChecks;

/*
    //?     Watches free space on the drive hosting the application — full disks take down
    //?     logging, temp files and (in single-host setups) the database before anything else.
    //?     Degraded < MinimumFreeDiskGb; Unhealthy < half of it.
*/
internal sealed class DiskSpaceHealthCheck(IOptions<HealthOptions> options) : IHealthCheck
{
    private readonly HealthOptions _options = options.Value;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var drive = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "/");
        double freeGb = drive.AvailableFreeSpace / 1_073_741_824d;

        string description =
            $"{freeGb:F1} GB free on '{drive.Name}' (threshold {_options.MinimumFreeDiskGb} GB).";

        HealthCheckResult result = freeGb switch
        {
            _ when freeGb < _options.MinimumFreeDiskGb / 2d
                => HealthCheckResult.Unhealthy(description),
            _ when freeGb < _options.MinimumFreeDiskGb
                => HealthCheckResult.Degraded(description),
            _ => HealthCheckResult.Healthy(description),
        };

        return Task.FromResult(result);
    }
}
