using Hangfire;
using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Domain;
using TransactionalBusiness.Api.Services;

namespace TransactionalBusiness.Api.Jobs;

public class StuckTransactionRecoveryJob
{
    
 private readonly PaymentDbContext _db;
 private readonly ILogger<StuckTransactionRecoveryJob> _logger;
 public StuckTransactionRecoveryJob(
        PaymentDbContext db, 
        ILogger<StuckTransactionRecoveryJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddMinutes(-15);

        var stuckTransactions = await _db.Transactions
            .Where(t => 
                (t.Status == TransactionStatus.Processing || 
                 t.Status == TransactionStatus.RetryScheduled)
                && t.LastAttemptAt < threshold)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} stuck transactions", 
            stuckTransactions.Count);

        foreach (var transaction in stuckTransactions)
        {
            _logger.LogWarning(
                "Recovering stuck transaction {Id} stuck in {Status} since {LastAttemptAt}",
                transaction.Id, transaction.Status, transaction.LastAttemptAt);

            var nextRetry = DateTime.UtcNow.AddSeconds(30);
            transaction.ScheduleRetry("Stuck transaction recovery", nextRetry);
            
            BackgroundJob.Schedule<RetryTransactionJob>(
                job => job.ExecuteAsync(transaction.Id),
                TimeSpan.FromSeconds(30));
        }

        await _db.SaveChangesAsync();
    }



}