using System;

namespace CoinPulse.Core.Interfaces;

public interface ICacheService
{
    // Cache'den veriyi alma
    Task<T?> GetAsync<T>(string key);

    // Cache'e veri yaz (expiration: ne kadar süreyle saklanacağı)
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    // Cache'den veriyi silme
    Task RemoveAsync(string key);
}
