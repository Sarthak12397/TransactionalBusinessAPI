using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Common.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire configuration for background job processing
/// Handles: retries, scheduled jobs, recurring jobs, dead-letter queues
/// </summary>
public static class HangfireConfiguration
{
    public static IServiceCollection AddHangfireBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Database connection string not found");

        // Configure Hangfire with PostgreSQL storage
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                })
                .UseSerilogLogProvider();

            // Global job filters
            config.UseFilter(new AutomaticRetryAttribute
            {
                Attempts = 5,
                DelaysInSeconds = new[] { 60, 300, 900, 3600, 7200 }, // 1min, 5min, 15min, 1hr, 2hr
                OnAttemptsExceeded = AttemptsExceededAction.Delete
            });
        });

        // Add Hangfire server with custom queues
        services.AddHangfireServer(options =>
        {
            options.Queues = new[]
            {
                "critical",      // Payment processing - highest priority
                "default",       // Normal operations
                "reconciliation", // Background reconciliation
                "dead-letter"    // Failed jobs for manual review
            };

            options.WorkerCount = Environment.ProcessorCount * 2;
            options.ServerName = $"{Environment.MachineName}-payment-worker";
        });

        return services;
    }

    /// <summary>
    /// Setup recurring jobs (runs once at startup)
    /// </summary>
    public static void ConfigureRecurringJobs()
    {
        // Reconciliation job - runs every 30 minutes
        RecurringJob.AddOrUpdate<IReconciliationJob>(
            "reconcile-transactions",
            job => job.ReconcileTransactionsAsync(CancellationToken.None),
            "*/30 * * * *", // Every 30 minutes
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "reconciliation"
            });

        // Retry failed transactions - runs every 5 minutes
        RecurringJob.AddOrUpdate<IRetryFailedTransactionsJob>(
            "retry-failed-transactions",
            job => job.RetryFailedTransactionsAsync(CancellationToken.None),
            "*/5 * * * *", // Every 5 minutes
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "default"
            });

        // Check for stuck transactions - runs every 15 minutes
        RecurringJob.AddOrUpdate<IStuckTransactionMonitorJob>(
            "monitor-stuck-transactions",
            job => job.MonitorStuckTransactionsAsync(CancellationToken.None),
            "*/15 * * * *", // Every 15 minutes
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "critical"
            });

        // Daily report - runs at 2 AM UTC
        RecurringJob.AddOrUpdate<IDailyReportJob>(
            "daily-transaction-report",
            job => job.GenerateDailyReportAsync(CancellationToken.None),
            "0 2 * * *", // 2 AM UTC daily
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "default"
            });
    }
}

// ========================================
// JOB INTERFACES (implement these!)
// ========================================

public interface IReconciliationJob
{
    Task ReconcileTransactionsAsync(CancellationToken cancellationToken);
}

public interface IRetryFailedTransactionsJob
{
    Task RetryFailedTransactionsAsync(CancellationToken cancellationToken);
}

public interface IStuckTransactionMonitorJob
{
    Task MonitorStuckTransactionsAsync(CancellationToken cancellationToken);
}

public interface IDailyReportJob
{
    Task GenerateDailyReportAsync(CancellationToken cancellationToken);
}
