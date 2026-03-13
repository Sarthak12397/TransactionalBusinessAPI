
using TransactionalBusiness.Api.Domain;
using TransactionalBusiness.Api.Services;

namespace TransactionalBusiness.Api.Services;
public class TransactionService : ITransactionService
{
    public Task<Transaction> CreateAsync(Guid userId, decimal amount, string currency, string idempotencyKey, string description)
    {
        throw new NotImplementedException();
    }

    public Task FailAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Transaction> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task SubmitAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}