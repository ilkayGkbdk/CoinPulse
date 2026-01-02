using System;
using CoinPulse.Core.Entities;

namespace CoinPulse.Core.Interfaces;

public interface ISearchService
{
    // Yeni bir fiyatın verisini indexle
    Task IndexPriceAsync(CryptoPrice price);

    // Belirli bir tarih aralığındaki fiyatları getir
    Task<IEnumerable<CryptoPrice>> SearchPricesAsync(string symbol, DateTime from, DateTime to);
}
