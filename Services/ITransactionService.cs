public interface ItransactionService
{
    Task<Transaction> CreateAsync(
        Guid userId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string description

    );


      Task<Transaction> GetByIdAsync(
        Guid userId
      );

         Task<Transaction> SubmitAsync(
                    Guid userId

         );


    
     Task<Transaction> FailAsync(
                Guid userId

     );


}