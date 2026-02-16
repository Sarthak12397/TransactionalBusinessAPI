using Microsoft.EntityFrameworkCore;
using PaymentSystem.Domain.Entities;

namespace PaymentSystem.Infrastructure.Database;

public class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transaction entity configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint on IdempotencyKey (CRITICAL!)
            entity.HasIndex(e => e.IdempotencyKey)
                .IsUnique();

            // Money value object
            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.AmountInCents)
                    .HasColumnName("AmountInCents")
                    .IsRequired();
            });

            // Required fields
            entity.Property(e => e.TransactionNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.CustomerId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Optional fields
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.ProcessorTransactionId)
                .HasMaxLength(100);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(1000);
        });
    }
}
