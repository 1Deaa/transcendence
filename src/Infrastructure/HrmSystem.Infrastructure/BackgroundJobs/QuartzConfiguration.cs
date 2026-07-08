using HrmSystem.Infrastructure.HealthChecks;
using HrmSystem.Infrastructure.Services.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     Central Quartz wiring (DevHabit pattern) — jobs resolve their dependencies from
    //?     the MS DI container, one scoped instance per execution.
    //!     Phase 1 uses the in-memory RAMJobStore: a restart loses in-flight triggers
    //!     (recurring schedules re-register at boot). The ADO job store is the Phase 2 upgrade.
*/
internal static class QuartzConfiguration
{
    internal static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        BackupOptions backupOptions =
            configuration.GetSection(BackupOptions.SectionName).Get<BackupOptions>()
            ?? new BackupOptions();
        HealthOptions healthOptions =
            configuration.GetSection(HealthOptions.SectionName).Get<HealthOptions>()
            ?? new HealthOptions();

        services.AddQuartz(quartz =>
        {
            quartz.AddJob<DatabaseBackupJob>(options => options.WithIdentity(DatabaseBackupJob.Key));
            quartz.AddTrigger(trigger =>
                trigger
                    .ForJob(DatabaseBackupJob.Key)
                    .WithIdentity("database-backup-trigger", "operations")
                    .WithCronSchedule(backupOptions.BackupCron)
            );

            quartz.AddJob<BackupRetentionJob>(options =>
                options.WithIdentity(BackupRetentionJob.Key)
            );
            quartz.AddTrigger(trigger =>
                trigger
                    .ForJob(BackupRetentionJob.Key)
                    .WithIdentity("backup-retention-trigger", "operations")
                    .WithCronSchedule(backupOptions.RetentionCron)
            );

            /*
                //?     ProcessImportJob has NO recurring trigger — it is fired on demand by
                //?     QuartzImportScheduler with the ImportJobId in the trigger data map.
                //!     StoreDurably is required for a job that exists without a trigger.
            */
            quartz.AddJob<ProcessImportJob>(options =>
                options.WithIdentity(ProcessImportJob.Key).StoreDurably()
            );

            quartz.AddJob<CleanupImportJobsJob>(options =>
                options.WithIdentity(CleanupImportJobsJob.Key)
            );
            quartz.AddTrigger(trigger =>
                trigger
                    .ForJob(CleanupImportJobsJob.Key)
                    .WithIdentity("cleanup-imports-trigger", "operations")
                    .WithCronSchedule("0 0 3 * * ?")
            );

            quartz.AddJob<HealthProbeJob>(options => options.WithIdentity(HealthProbeJob.Key));
            quartz.AddTrigger(trigger =>
                trigger
                    .ForJob(HealthProbeJob.Key)
                    .WithIdentity("health-probe-trigger", "operations")
                    .WithSimpleSchedule(schedule =>
                        schedule
                            .WithIntervalInSeconds(healthOptions.ProbeIntervalSeconds)
                            .RepeatForever()
                    )
                    //? First probe shortly after boot so the status page fills fast.
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(10))
            );
        });

        //! WaitForJobsToComplete — a deploy/restart lets a running backup finish instead of killing it.
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
