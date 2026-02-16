namespace Modules.Transactions.Domain.ValueObjects;

/// <summary>
/// Money value object - handles currency amounts properly
/// Prevents primitive obsession (using bare decimals)
/// </summary>
public sealed record Money
{
    /// <summary>
    /// Amount in smallest currency unit (e.g., cents for USD)
    /// Using long to avoid floating point precision issues
    /// $10.50 = 1050 cents
    /// </summary>
    public long AmountInCents { get; init; }

    /// <summary>
    /// Amount as decimal for display (e.g., 10.50)
    /// </summary>
    public decimal Amount => AmountInCents / 100m;

    private Money(long amountInCents)
    {
        if (amountInCents < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amountInCents));
        }

        AmountInCents = amountInCents;
    }

    /// <summary>
    /// Create from cents (internal storage format)
    /// </summary>
    public static Money FromCents(long cents) => new(cents);

    /// <summary>
    /// Create from decimal amount (e.g., 10.50)
    /// </summary>
    public static Money FromAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        var cents = (long)(amount * 100);
        return new Money(cents);
    }

    /// <summary>
    /// Create zero amount
    /// </summary>
    public static Money Zero => new(0);

    // Operator overloads for convenience
    public static Money operator +(Money left, Money right) 
        => new(left.AmountInCents + right.AmountInCents);

    public static Money operator -(Money left, Money right) 
        => new(left.AmountInCents - right.AmountInCents);

    public static bool operator >(Money left, Money right) 
        => left.AmountInCents > right.AmountInCents;

    public static bool operator <(Money left, Money right) 
        => left.AmountInCents < right.AmountInCents;

    public static bool operator >=(Money left, Money right) 
        => left.AmountInCents >= right.AmountInCents;

    public static bool operator <=(Money left, Money right) 
        => left.AmountInCents <= right.AmountInCents;

    public override string ToString() => $"{Amount:F2}";
}
