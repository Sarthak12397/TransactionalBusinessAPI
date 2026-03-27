using TransactionalBusiness.Api.Domain;

namespace TransactionalBusiness.Api.Services;


public interface ITransactionService
{
    Task<Transaction> CreateAsync(
        Guid userId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string description

    );


      Task<Transaction> GetByIdAsync(
        Guid id
      );

         Task SubmitAsync(
                    Guid id

         );


    
     Task FailAsync(  Guid id);

     Task CompleteAsync(Guid id);
     Task ProcessAsync(Guid id);




}