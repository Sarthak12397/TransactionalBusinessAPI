using Modules.Common.Domain.Results;
using Modules.Transactions.Domain.Enums;
using Modules.Transactions.Domain.ValueObjects;

namespace Modules.Transactions.Domain.Entities;

/// <summary>
/// Transaction aggregate root - represents a payment transaction lifecycle
/// </summary>
public sealed class Transaction
{
    private const string ErrorCode = "Transactions.Validation";

    // ========================================
    // IDENTITY & IDEMPOTENCY
    // ========================================
    
    /// <summary>
    /// Internal database ID
    /// </summary>
    public Guid Id { get; private init; }
    
    /// <summary>
    /// Public-facing transaction reference (e.g., "TXN-ABC123")
    /// User-visible, encoded, non-guessable
    /// </summary>
    public string TransactionNumber { get; private set; } = null!;
    
    /// <summary>
    /// CRITICAL: Idempotency key to prevent duplicate charges
    /// Client provides this (e.g., UUID from their system)
    /// Database should have UNIQUE constraint on this
    /// </summary>
    public string IdempotencyKey { get; private set; } = null!;

    // ========================================
    // BUSINESS DATA
    // ========================================
    
    public Money Amount { get; private set; } = null!;
    
    public string Currency { get; private set; } = null!;
    
    /// <summary>
    /// Customer/merchant identifier
    /// </summary>
    public string CustomerId { get; private set; } = null!;
    
    /// <summary>
    /// External order/invoice ID that triggered this payment
    /// </summary>
    public string OrderId { get; private set; } = null!;
    
    /// <summary>
    /// Payment method (credit_card, bank_transfer, etc.)
    /// </summary>
    public string PaymentMethod { get; private set; } = null!;
    
    public string? Description { get; private set; }

    // ========================================
    // STATE MACHINE
    // ========================================
    
    public TransactionStatus Status { get; private set; }
    
    /// <summary>
    /// Number of times we've attempted to process this transaction
    /// Used for retry logic and dead-letter detection
    /// </summary>
    public int AttemptCount { get; private set; }
    
    /// <summary>
    /// Maximum retry attempts before giving up
    /// </summary>
    private const int MaxAttempts = 5;

    // ========================================
    // EXTERNAL SYSTEM INTEGRATION
    // ========================================
    
    /// <summary>
    /// External payment processor's transaction ID (e.g., Stripe charge ID)
    /// null until we actually send to processor
    /// </summary>
    public string? ProcessorTransactionId { get; private set; }
    
    /// <summary>
    /// Response/error code from payment processor
    /// </summary>
    public string? ProcessorResponseCode { get; private set; }
    
    /// <summary>
    /// Raw response from processor (for debugging/reconciliation)
    /// </summary>
    public string? ProcessorRawResponse { get; private set; }

    // ========================================
    // FAILURE & RECONCILIATION
    // ========================================
    
    /// <summary>
    /// Human-readable failure reason
    /// </summary>
    public string? FailureReason { get; private set; }
    
    /// <summary>
    /// When this transaction should be reconciled with external system
    /// null = no reconciliation needed
    /// </summary>
    public DateTime? ScheduledReconciliationAt { get; private set; }
    
    /// <summary>
    /// Last time we attempted reconciliation
    /// </summary>
    public DateTime? LastReconciledAt { get; private set; }

    // ========================================
    // AUDIT TRAIL (Critical for payments!)
    // ========================================
    
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }
    
    /// <summary>
    /// When we started processing (sent to payment processor)
    /// </summary>
    public DateTime? ProcessingStartedAt { get; private set; }
    
    /// <summary>
    /// When transaction reached terminal state (Completed/Failed/Refunded)
    /// </summary>
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Last retry attempt timestamp
    /// </summary>
    public DateTime? LastAttemptAt { get; private set; }

    // ========================================
    // EF CORE CONSTRUCTOR
    // ========================================
    
    private Transaction()
    {
    }

    // ========================================
    // FACTORY METHOD
    // ========================================
    
    public static Transaction Create(
        string transactionNumber,
        string idempotencyKey,
        Money amount,
        string currency,
        string customerId,
        string orderId,
        string paymentMethod,
        string? description = null)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = transactionNumber,
            IdempotencyKey = idempotencyKey,
            Amount = amount,
            Currency = currency,
            CustomerId = customerId,
            OrderId = orderId,
            PaymentMethod = paymentMethod,
            Description = description,
            Status = TransactionStatus.Pending,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        return transaction;
    }

    // ========================================
    // STATE TRANSITIONS (State Machine)
    // ========================================
    
    /// <summary>
    /// Transition to Processing - we're sending to payment processor
    /// </summary>
    public Result<Success> StartProcessing()
    {
        if (Status is not TransactionStatus.Pending and not TransactionStatus.RetryScheduled)
        {
            return Error.Validation(
                ErrorCode, 
                $"Can only start processing from Pending or RetryScheduled status. Current: {Status}");
        }

        Status = TransactionStatus.Processing;
        ProcessingStartedAt ??= DateTime.UtcNow;
        LastAttemptAt = DateTime.UtcNow;
        AttemptCount++;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    /// <summary>
    /// Mark as successfully completed
    /// </summary>
    public Result<Success> MarkAsCompleted(
        string processorTransactionId, 
        string processorResponseCode,
        string? processorRawResponse = null)
    {
        if (Status is not TransactionStatus.Processing)
        {
            return Error.Validation(
                ErrorCode, 
                $"Can only complete from Processing status. Current: {Status}");
        }

        Status = TransactionStatus.Completed;
        ProcessorTransactionId = processorTransactionId;
        ProcessorResponseCode = processorResponseCode;
        ProcessorRawResponse = processorRawResponse;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    /// <summary>
    /// Mark as failed - either permanently or for retry
    /// </summary>
    public Result<Success> MarkAsFailed(
        string failureReason,
        string? processorResponseCode = null,
        bool isRetryable = true)
    {
        if (Status is not TransactionStatus.Processing)
        {
            return Error.Validation(
                ErrorCode, 
                $"Can only fail from Processing status. Current: {Status}");
        }

        FailureReason = failureReason;
        ProcessorResponseCode = processorResponseCode;
        UpdatedAt = DateTime.UtcNow;

        // Should we retry or give up?
        if (isRetryable && AttemptCount < MaxAttempts)
        {
            Status = TransactionStatus.RetryScheduled;
            // Schedule next retry with exponential backoff
            var delayMinutes = Math.Pow(2, AttemptCount); // 2, 4, 8, 16, 32 minutes
            ScheduledReconciliationAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            Status = TransactionStatus.Failed;
            CompletedAt = DateTime.UtcNow;
        }

        return Result.Success;
    }

    /// <summary>
    /// Initiate a refund
    /// </summary>
    public Result<Success> InitiateRefund()
    {
        if (Status is not TransactionStatus.Completed)
        {
            return Error.Validation(
                ErrorCode, 
                $"Can only refund Completed transactions. Current: {Status}");
        }

        Status = TransactionStatus.RefundPending;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    /// <summary>
    /// Mark refund as completed
    /// </summary>
    public Result<Success> CompleteRefund(
        string processorRefundId,
        string processorResponseCode)
    {
        if (Status is not TransactionStatus.RefundPending)
        {
            return Error.Validation(
                ErrorCode, 
                $"Can only complete refund from RefundPending status. Current: {Status}");
        }

        Status = TransactionStatus.Refunded;
        ProcessorTransactionId = processorRefundId;
        ProcessorResponseCode = processorResponseCode;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    /// <summary>
    /// Cancel pending transaction
    /// </summary>
    public Result<Success> Cancel()
    {
        if (Status is TransactionStatus.Completed or TransactionStatus.Refunded)
        {
            return Error.Validation(
                ErrorCode, 
                $"Cannot cancel a {Status} transaction");
        }

        Status = TransactionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    // ========================================
    // RECONCILIATION
    // ========================================
    
    /// <summary>
    /// Update reconciliation timestamp
    /// </summary>
    public void MarkAsReconciled()
    {
        LastReconciledAt = DateTime.UtcNow;
        ScheduledReconciliationAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedule next reconciliation check
    /// </summary>
    public void ScheduleReconciliation(DateTime scheduledAt)
    {
        ScheduledReconciliationAt = scheduledAt;
        UpdatedAt = DateTime.UtcNow;
    }

    // ========================================
    // BUSINESS RULES / QUERIES
    // ========================================
    
    /// <summary>
    /// Is this transaction in a terminal state?
    /// </summary>
    public bool IsTerminal => Status is 
        TransactionStatus.Completed or 
        TransactionStatus.Failed or 
        TransactionStatus.Refunded or 
        TransactionStatus.Cancelled;

    /// <summary>
    /// Is this transaction eligible for retry?
    /// </summary>
    public bool CanRetry => 
        Status is TransactionStatus.RetryScheduled && 
        AttemptCount < MaxAttempts;

    /// <summary>
    /// Should this transaction be sent to dead-letter queue?
    /// </summary>
    public bool IsDeadLetter => 
        Status is TransactionStatus.Failed && 
        AttemptCount >= MaxAttempts;

    /// <summary>
    /// Is this transaction stuck in processing? (danger zone)
    /// </summary>
    public bool IsStuckInProcessing(TimeSpan timeout)
    {
        return Status is TransactionStatus.Processing &&
               ProcessingStartedAt.HasValue &&
               DateTime.UtcNow - ProcessingStartedAt.Value > timeout;
    }
}
