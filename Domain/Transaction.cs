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

    public TransactionStatus Status
    {
        get; private set;
    }

    
   public Transaction(


  Guid userId,
    decimal amount,
    string currencies,
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
    Currency = currencies;
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
  if(Status != TransactionStatus.Submitted)
        {
            throw new InvalidOperationException($"Cannot Process from {Status}");
        }
        Status = TransactionStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
}

public void Complete()
{
     if(Status != TransactionStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot Complete from {Status}");
        }
        Status = TransactionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
}

public void Fail()
{
  if(Status != TransactionStatus.Submitted && Status != TransactionStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot Fail from {Status}");
        }
        Status = TransactionStatus.Failed;
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


