namespace FuelTrack.Domain.MultiTenancy;

public sealed class SubscriptionPlan
{
    private SubscriptionPlan() { }
    public SubscriptionPlan(Guid id, string name, decimal price, string currency)
    {
        if (id == Guid.Empty) throw new ArgumentException("Plan ID is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        currency = currency.Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));
        Id = id;
        Name = name.Trim();
        Price = price;
        Currency = currency;
        IsActive = true;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "";
    public bool IsActive { get; private set; }
}
