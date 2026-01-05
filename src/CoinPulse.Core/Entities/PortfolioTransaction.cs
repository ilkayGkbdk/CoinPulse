using System;
using CoinPulse.Core.Common;

namespace CoinPulse.Core.Entities;

public class PortfolioTransaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty; // İşlemi gerçekleştiren kullanıcı ID'si
    public string Symbol { get; set; } = string.Empty; // Hangi coini aldı
    public decimal Amount { get; set; } // Alınan coin miktarı
    public decimal BuyPrice { get; set; } // o anki fiyat
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow; // İşlem tarihi
    public decimal TotalCost => Amount * BuyPrice; // Toplam maliyet
}
