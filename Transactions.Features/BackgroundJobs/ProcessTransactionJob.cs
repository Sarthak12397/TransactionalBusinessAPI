using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Transactions.Domain.Enums;
using Modules.Transactions.Infrastructure.Database;
using Modules.Transactions.Infrastructure.PaymentProcessors;

namespace Modules.Transactions.Features.BackgroundJobs;

/// <summary>
/// Background job that processes a transaction through payment processor
/// This is the core async job that handles payment lifecycle
/// </summary>
public interface IProcessTransactionJob
{
    Task ExecuteAsync(Guid transactionId, CancellationToken cancellationToken);
}

public sealed class ProcessTransactionJob(
    TransactionsDbContext dbContext,
    IPaymentProcessorService paymentProcessor,
    ILogger<ProcessTransactionJob> logger) 
    : IProcessTransactionJob
{
    /// <summary>
    /// Process a single transaction
    /// Called by Hangfire either immediately or on retry schedule
    /// </summary>
    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves in the domain
    [Queue("critical")]
    public async Task ExecuteAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing transaction {TransactionId}", transactionId);

        // Load transaction with pessimistic lock to prevent concurrent processing
        var transaction = await dbContext.Transactions
            .Where(t => t.Id == transactionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            logger.LogWarning("Transaction {TransactionId} not found", transactionId);
            return;
        }

        // Check if already in terminal state (idempotency check)
        if (transaction.IsTerminal)
        {
            logger.LogInformation(
                "Transaction {TransactionId} already in terminal state {Status}", 
                transactionId, 
                transaction.Status);
            return;
        }

        // Transition to Processing state
        var processingResult = transaction.StartProcessing();
        if (processingResult.IsFailure)
        {
            logger.LogWarning(
                "Cannot start processing transaction {TransactionId}: {Error}", 
                transactionId, 
                processingResult.Errors);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Call external payment processor (Stripe, etc.)
            logger.LogInformation(
                "Calling payment processor for transaction {TransactionId}", 
                transactionId);

            var processorResult = await paymentProcessor.ProcessPaymentAsync(
                transaction,
                cancellationToken);

            if (processorResult.IsSuccess)
            {
                // SUCCESS! Mark as completed
                var completedResult = transaction.MarkAsCompleted(
                    processorTransactionId: processorResult.Value!.TransactionId,
                    processorResponseCode: processorResult.Value.ResponseCode,
                    processorRawResponse: processorResult.Value.RawResponse);

                if (completedResult.IsSuccess)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    logger.LogInformation(
                        "Transaction {TransactionId} completed successfully. Processor ID: {ProcessorId}",
                        transactionId,
                        processorResult.Value.TransactionId);

                    // TODO: Publish TransactionCompletedEvent for downstream systems
                }
            }
            else
            {
                // FAILURE - determine if retryable
                var isRetryable = processorResult.Errors
                    .Any(e => e.Type == Common.Domain.Results.ErrorType.Transient);

                var failureReason = string.Join("; ", processorResult.Errors.Select(e => e.Description));

                var failedResult = transaction.MarkAsFailed(
                    failureReason: failureReason,
                    processorResponseCode: processorResult.Errors.FirstOrDefault()?.Code,
                    isRetryable: isRetryable);

                await dbContext.SaveChangesAsync(cancellationToken);

                if (transaction.Status == TransactionStatus.RetryScheduled)
                {
                    // Schedule retry job
                    var delayMinutes = Math.Pow(2, transaction.AttemptCount);
                    var retryDelay = TimeSpan.FromMinutes(delayMinutes);

                    logger.LogWarning(
                        "Transaction {TransactionId} failed (attempt {Attempt}). Scheduling retry in {Delay} minutes",
                        transactionId,
                        transaction.AttemptCount,
                        delayMinutes);

                    BackgroundJob.Schedule<IProcessTransactionJob>(
                        job => job.ExecuteAsync(transactionId, CancellationToken.None),
                        retryDelay);
                }
                else
                {
                    // Max retries exhausted - send to dead-letter queue
                    logger.LogError(
                        "Transaction {TransactionId} permanently failed after {Attempts} attempts. Reason: {Reason}",
                        transactionId,
                        transaction.AttemptCount,
                        failureReason);

                    // Schedule for manual review
                    BackgroundJob.Enqueue<IDeadLetterQueueHandler>(
                        handler => handler.HandleAsync(transactionId, CancellationToken.None));
                }
            }
        }
        catch (Exception ex)
        {
            // Unexpected exception - mark as failed and schedule retry
            logger.LogError(
                ex,
                "Unexpected error processing transaction {TransactionId}",
                transactionId);

            var failedResult = transaction.MarkAsFailed(
                failureReason: $"Unexpected error: {ex.Message}",
                isRetryable: true);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction.CanRetry)
            {
                var delayMinutes = Math.Pow(2, transaction.AttemptCount);
                BackgroundJob.Schedule<IProcessTransactionJob>(
                    job => job.ExecuteAsync(transactionId, CancellationToken.None),
                    TimeSpan.FromMinutes(delayMinutes));
            }
            else
            {
                BackgroundJob.Enqueue<IDeadLetterQueueHandler>(
                    handler => handler.HandleAsync(transactionId, CancellationToken.None));
            }

            throw; // Re-throw for Hangfire to log
        }
    }
}

/// <summary>
/// Handles transactions that have exhausted retry attempts
/// Manual intervention required
/// </summary>
public interface IDeadLetterQueueHandler
{
    Task HandleAsync(Guid transactionId, CancellationToken cancellationToken);
}

public sealed class DeadLetterQueueHandler(
    TransactionsDbContext dbContext,
    ILogger<DeadLetterQueueHandler> logger) 
    : IDeadLetterQueueHandler
{
    [Queue("dead-letter")]
    public async Task HandleAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions.FindAsync([transactionId], cancellationToken);
        
        if (transaction is null)
        {
            logger.LogWarning("Transaction {TransactionId} not found in dead-letter queue", transactionId);
            return;
        }

        logger.LogCritical(
            "Transaction {TransactionId} in DEAD LETTER QUEUE. Status: {Status}, Attempts: {Attempts}, Reason: {Reason}",
            transaction.Id,
            transaction.Status,
            transaction.AttemptCount,
            transaction.FailureReason);

        // TODO: Send alert to operations team
        // TODO: Create ticket in support system
        // TODO: Log to special monitoring dashboard
    }
}
