
namespace TransactionalBusiness.Api.Domain;

public class Transaction
{
    public Guid Id
    {
        get; private set;
    }
    public Guid UserId
    {
        get; private set;
    }
    public decimal Amount
    {
        get;
        private set;

    }
    public string Currency
    {
        get; private set;
    }

    public DateTime CreatedAt
    {
        get; private set;
    }
    public DateTime? UpdatedAt
    {
        get; private set;
    }
    public string IdempotencyKey
    {
        get; private set;
    }

    public string Description
    {
        get; private set;
    }

    public int RetryCount{
        get;
        private set;
    }
    public string? FailureReason
    {
        get;private set;
    }

public DateTime? LastAttemptAt { get; private set; }


    public TransactionStatus Status
    {
        get; private set;
    }
public DateTime? NextRetryAt { get; private set; }
public int MaxRetries { get; private set; } = 3;
    

public void ScheduleRetry(string reason, DateTime nextRetryAt)
{
    if (Status != TransactionStatus.Processing)
    {
        throw new InvalidOperationException($"Cannot retry from {Status}");
    }

    if (RetryCount >= MaxRetries)
    {
        Status = TransactionStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
        return;
    }

    FailureReason = reason;
    NextRetryAt = nextRetryAt;
    Status = TransactionStatus.RetryScheduled;
    UpdatedAt = DateTime.UtcNow;
}








   public Transaction(


  Guid userId,
    decimal amount,
    string currency,
    string idempotencyKey,
    string description

   )
    {
        if(amount <= 0)
        {
            throw new ArgumentException("Amount should be greater than zero");
        }
        Id = Guid.NewGuid();
         UserId = userId;
         Amount = amount;
    Currency = currency;
    IdempotencyKey = idempotencyKey;
    Description = description;
    Status = TransactionStatus.Pending;
    CreatedAt = DateTime.UtcNow;  
    UpdatedAt = null;      

        
    }




    public void Submit()
{

     if(Status != TransactionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot Submit from {Status}");
        }
        Status = TransactionStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;

}

public void Process()
{
    if (Status != TransactionStatus.Submitted 
        && Status != TransactionStatus.RetryScheduled)
    {
        throw new InvalidOperationException($"Cannot Process from {Status}");
    }

    Status = TransactionStatus.Processing;

    RetryCount++;                 
    LastAttemptAt = DateTime.UtcNow;
    NextRetryAt = null;            
    UpdatedAt = DateTime.UtcNow;
}

public void Complete()
{
    if (Status != TransactionStatus.Processing)
    {
        throw new InvalidOperationException($"Cannot Complete from {Status}");
    }

    Status = TransactionStatus.Completed;
    FailureReason = null; // ✅ important
    UpdatedAt = DateTime.UtcNow;
}
public void Fail(string reason)
{
    if (Status != TransactionStatus.Submitted 
        && Status != TransactionStatus.Processing)
    {
        throw new InvalidOperationException($"Cannot Fail from {Status}");
    }

    Status = TransactionStatus.Failed;
    FailureReason = reason;
    LastAttemptAt = DateTime.UtcNow; // ✅ add this
    UpdatedAt = DateTime.UtcNow;
}

public void Reverse()
{

  if(Status != TransactionStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot Reverse from {Status}");
        }
        Status = TransactionStatus.Reversed;
        UpdatedAt = DateTime.UtcNow;
}

}


