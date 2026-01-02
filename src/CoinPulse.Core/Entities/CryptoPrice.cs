using System;

namespace CoinPulse.Core.Entities;

public class CryptoPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty; // e.g., BTC, ETH
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Time of the price record
}
