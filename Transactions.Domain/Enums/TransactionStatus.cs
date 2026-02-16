namespace Modules.Transactions.Domain.Enums;

/// <summary>
/// Transaction lifecycle states
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Initial state - created but not yet sent to processor
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Currently being processed by payment processor
    /// DANGER: If stuck here too long, needs reconciliation
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Successfully completed and funds captured
    /// TERMINAL STATE
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Failed after all retry attempts exhausted
    /// TERMINAL STATE
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Failed but scheduled for retry
    /// Will transition back to Processing
    /// </summary>
    RetryScheduled = 4,
    
    /// <summary>
    /// Refund initiated but not yet confirmed by processor
    /// </summary>
    RefundPending = 5,
    
    /// <summary>
    /// Refund completed
    /// TERMINAL STATE
    /// </summary>
    Refunded = 6,
    
    /// <summary>
    /// Cancelled before completion
    /// TERMINAL STATE
    /// </summary>
    Cancelled = 7
}
