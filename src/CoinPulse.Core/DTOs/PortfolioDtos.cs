using System;

namespace CoinPulse.Core.DTOs;

// Price ve TransactionDate artık opsiyonel (?)
public record BuyCryptoDto(
    string Symbol,
    decimal Amount,
    decimal? Price = null,
    DateTime? TransactionDate = null
);

// PortfolioItemDto aynen kalıyor...
public class PortfolioItemDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercentage { get; set; }
}