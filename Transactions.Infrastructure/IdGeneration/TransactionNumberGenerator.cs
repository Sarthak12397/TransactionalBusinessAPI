using HashidsNet;
using IdGen;

namespace Modules.Transactions.Infrastructure.IdGeneration;

/// <summary>
/// Generates human-readable transaction numbers like "TXN-ABC123"
/// Uses IdGen for distributed ID generation + Hashids for encoding
/// </summary>
public interface ITransactionNumberGenerator
{
    string Generate();
}

public sealed class TransactionNumberGenerator : ITransactionNumberGenerator
{
    private readonly IdGenerator _idGenerator;
    private readonly Hashids _hashids;

    public TransactionNumberGenerator()
    {
        // IdGen configuration
        // generatorId should be unique per service instance (0-1023)
        // In production, get this from configuration/environment
        var generatorId = 0;
        var epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        _idGenerator = new IdGenerator(generatorId, epoch);

        // Hashids configuration for encoding IDs into readable strings
        _hashids = new Hashids(
            salt: "payment-system-secret-salt", // Change this in production!
            minHashLength: 8,
            alphabet: "ABCDEFGHJKLMNPQRSTUVWXYZ23456789" // No confusing characters (0,O,1,I)
        );
    }

    public string Generate()
    {
        // Generate distributed ID (long)
        var id = _idGenerator.CreateId();

        // Encode to readable string
        var encoded = _hashids.EncodeLong(id);

        // Prefix for clarity
        return $"TXN-{encoded}";
    }
}

// ========================================
// ALTERNATIVE: Simple Sequential Generator
// (For single-instance deployments)
// ========================================

public sealed class SimpleTransactionNumberGenerator : ITransactionNumberGenerator
{
    private static long _counter = 0;

    public string Generate()
    {
        var id = Interlocked.Increment(ref _counter);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"TXN-{timestamp}-{id:D6}";
        // Example: TXN-20250214-000001
    }
}
