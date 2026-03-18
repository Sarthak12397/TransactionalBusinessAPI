
using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Domain;
using TransactionalBusiness.Api.Services;

namespace TransactionalBusiness.Api.Services;
public class TransactionService : ITransactionService
{

    private readonly PaymentDbContext _db;


    public TransactionService(PaymentDbContext db)
    {
        _db = db;
    }
    public async Task<Transaction> CreateAsync(Guid userId, decimal amount, string currency, string idempotencyKey, string description)
    {

        var existing = await _db.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
        if(existing != null)
        {
            return existing;
        }

        var transaction = new Transaction(  userId,
            amount,
            currency,
            idempotencyKey,
            description    
        );

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return transaction;
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