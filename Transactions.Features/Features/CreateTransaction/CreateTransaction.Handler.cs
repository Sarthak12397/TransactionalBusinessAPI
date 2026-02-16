using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Common.Infrastructure.Outbox;
using Modules.Transactions.Domain.Entities;
using Modules.Transactions.Domain.ValueObjects;
using Modules.Transactions.Features.BackgroundJobs;
using Modules.Transactions.Features.Features.CreateTransaction.Events;
using Modules.Transactions.Infrastructure.Database;
using Modules.Transactions.Infrastructure.IdGeneration;

namespace Modules.Transactions.Features.Features.CreateTransaction;

internal interface ICreateTransactionHandler : IHandler
{
    Task<Result<TransactionResponse>> HandleAsync(
        CreateTransactionRequest request, 
        CancellationToken cancellationToken);
}

internal sealed class CreateTransactionHandler(
    TransactionsDbContext context,
    ITransactionNumberGenerator numberGenerator,
    ILogger<CreateTransactionHandler> logger)
    : ICreateTransactionHandler
{
    public async Task<Result<TransactionResponse>> HandleAsync(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        // ========================================
        // CRITICAL: IDEMPOTENCY CHECK
        // ========================================
        // Check if we've already processed this idempotency key
        var existingTransaction = await context.Transactions
            .FirstOrDefaultAsync(
                t => t.IdempotencyKey == request.IdempotencyKey, 
                cancellationToken);

        if (existingTransaction is not null)
        {
            logger.LogInformation(
                "Transaction with idempotency key {IdempotencyKey} already exists. Returning existing transaction {TransactionId}",
                request.IdempotencyKey,
                existingTransaction.Id);

            // Return the existing transaction - don't create duplicate!
            return existingTransaction.MapToResponse();
        }

        // ========================================
        // BUSINESS VALIDATION
        // ========================================
        
        // Validate amount
        if (request.Amount <= 0)
        {
            return Error.Validation("Transactions.InvalidAmount", "Amount must be greater than zero");
        }

        // TODO: Additional validations
        // - Check customer exists
        // - Verify payment method is valid
        // - Check for fraud rules
        // - Validate currency code

        // ========================================
        // CREATE TRANSACTION
        // ========================================
        
        var transactionNumber = numberGenerator.Generate();
        var money = Money.FromAmount(request.Amount);

        var transaction = Transaction.Create(
            transactionNumber: transactionNumber,
            idempotencyKey: request.IdempotencyKey,
            amount: money,
            currency: request.Currency,
            customerId: request.CustomerId,
            orderId: request.OrderId,
            paymentMethod: request.PaymentMethod,
            description: request.Description
        );

        // Add to database
        await context.Transactions.AddAsync(transaction, cancellationToken);

        // ========================================
        // OUTBOX PATTERN - Reliable Event Publishing
        // ========================================
        var transactionCreatedEvent = new TransactionCreatedEvent(
            TransactionId: transaction.Id,
            TransactionNumber: transaction.TransactionNumber,
            CustomerId: transaction.CustomerId,
            Amount: transaction.Amount.Amount,
            Currency: transaction.Currency,
            CreatedAt: transaction.CreatedAt
        );

        // Add event to outbox in SAME transaction
        await context.AddOutboxMessageAsync(transactionCreatedEvent, cancellationToken);

        // Save everything atomically
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created transaction {TransactionId} with number {TransactionNumber}",
            transaction.Id,
            transaction.TransactionNumber);

        // ========================================
        // BACKGROUND JOB - Process Payment
        // ========================================
        // Schedule the actual payment processing as background job
        // This returns immediately to the user while processing happens async
        BackgroundJob.Enqueue<IProcessTransactionJob>(
            job => job.ExecuteAsync(transaction.Id, CancellationToken.None));

        logger.LogInformation(
            "Enqueued background job to process transaction {TransactionId}",
            transaction.Id);

        return transaction.MapToResponse();
    }
}

// ========================================
// REQUEST & RESPONSE DTOs
// ========================================

public record CreateTransactionRequest(
    string IdempotencyKey,    // CRITICAL: Client must provide this!
    decimal Amount,
    string Currency,
    string CustomerId,
    string OrderId,
    string PaymentMethod,
    string? Description
);

public record TransactionResponse(
    Guid Id,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt
);

// ========================================
// MAPPING EXTENSIONS
// ========================================

public static class TransactionMappingExtensions
{
    public static TransactionResponse MapToResponse(this Transaction transaction)
    {
        return new TransactionResponse(
            Id: transaction.Id,
            TransactionNumber: transaction.TransactionNumber,
            Amount: transaction.Amount.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.ToString(),
            CreatedAt: transaction.CreatedAt
        );
    }
}
