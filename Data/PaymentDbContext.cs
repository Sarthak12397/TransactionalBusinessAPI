using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Domain;

namespace TransactionalBusiness.Api.Data;

public class PaymentDbContext :DbContext{
     public PaymentDbContext(DbContextOptions<PaymentDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
}