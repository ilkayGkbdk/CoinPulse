using System;

namespace CoinPulse.Core.Events;

public record PriceUpdatedEvent
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
