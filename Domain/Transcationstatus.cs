namespace TransactionalBusiness.Api.Domain;

    public enum TransactionStatus
    {
        Completed,
        Pending,
        Reversed,
        Submitted,
        Failed,
        Processing,
        RetryScheduled  
        
        
    }
