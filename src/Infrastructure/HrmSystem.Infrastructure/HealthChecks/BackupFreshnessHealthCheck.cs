using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HrmSystem.Infrastructure.HealthChecks;

/*
    //?     The disaster-recovery watchdog: how old is the newest SUCCESSFUL backup?
    //?      - Degraded  > BackupFreshDegradedHours  (one missed nightly run)
    //?      - Unhealthy > BackupFreshUnhealthyHours (two missed runs — RPO in danger)
    //!     "No backup yet" is Degraded, not Unhealthy — a fresh install must not page on-call.
*/
internal sealed class BackupFreshnessHealthCheck(
    ISqlConnectionFactory connectionFactory,
    IOptions<HealthOptions> options
) : IHealthCheck
{
    private readonly HealthOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT TOP 1 CompletedAtUtc
            FROM [HrmSystem].[BackupHistory]
            WHERE Status = 'Succeeded'
            ORDER BY CompletedAtUtc DESC
            """;

        var command = new CommandDefinition(commandText: sql, cancellationToken: cancellationToken);
        DateTime? lastSuccess = await connection.ExecuteScalarAsync<DateTime?>(command);

        if (lastSuccess is null)
        {
            return HealthCheckResult.Degraded("No successful backup has been recorded yet.");
        }

        double ageHours = (DateTime.UtcNow - lastSuccess.Value).TotalHours;
        string description =
            $"Last successful backup {ageHours:F1}h ago ({lastSuccess:yyyy-MM-dd HH:mm} UTC).";

        return ageHours switch
        {
            _ when ageHours > _options.BackupFreshUnhealthyHours
                => HealthCheckResult.Unhealthy(description),
            _ when ageHours > _options.BackupFreshDegradedHours
                => HealthCheckResult.Degraded(description),
            _ => HealthCheckResult.Healthy(description),
        };
    }
}
