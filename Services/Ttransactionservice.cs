
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

    public async Task FailAsync(Guid id)
    {

        var FailbyId = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);

        if(FailbyId == null)
        {
                        throw new KeyNotFoundException($"No {id} submitted");

        }

        FailbyId.Fail("Manual Fail");


        await  _db.SaveChangesAsync() ;                
    }

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        
              var getbyid = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if(getbyid == null)
        {
            throw new KeyNotFoundException($"Transaction {id} not found");

        }
        return getbyid;

    }

    public async Task SubmitAsync(Guid id)
    {
        
        var submitbyId = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);

        if(submitbyId == null)
        {
                        throw new KeyNotFoundException($"No {id} submitted");

        }

        submitbyId.Submit();


        await  _db.SaveChangesAsync() ;                

    }


       public async Task ProcessAsync(Guid id)
            {


                 var updated = await _db.Transactions
        .Where(t => t.Id == id 
               && (t.Status == TransactionStatus.Submitted 
               || t.Status == TransactionStatus.RetryScheduled))
        .ExecuteUpdateAsync(s => s
            .SetProperty(t => t.Status, TransactionStatus.Processing));

    if (updated == 0)
    {
        // another worker already claimed it — exit cleanly
        return;
    }

           var processbyid = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);
           if(processbyid == null)
        {
             throw new KeyNotFoundException($"No {id} submitted");

        }

             processbyid.Process();
            await _db.SaveChangesAsync();
        }

            public async Task CompleteAsync(Guid id)
            {
            var completebyid = await _db.Transactions.FirstOrDefaultAsync(t=> t.Id == id);

        if(completebyid == null)
        {
                        throw new KeyNotFoundException($"No {id} submitted");

        }

        completebyid.Complete();


        await  _db.SaveChangesAsync() ;               
            }
}