using System;
using StackExchange.Redis;

namespace CoinPulse.Infrastructure.Services;

public class SymbolService
{
    private readonly IDatabase _redis;
    private const string REDIS_KEY_SYMBOLS = "config:active_symbols";

    public SymbolService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Varsayılan coinleri yükle
    public async Task InitializeDefaultsAsync()
    {
        var defaults = new[] { "BTC", "ETH", "SOL", "AVAX", "XRP", "XAG", "XAU" }; // Gümüş (XAG) ve Altın (XAU) ekledik
        foreach (var symbol in defaults)
        {
            await AddSymbolAsync(symbol);
        }
    }

    // Listeye yeni sembol ekle
    public async Task AddSymbolAsync(string symbol)
    {
        // Redis Set yapısı duplicate (tekrar eden) kayıtları otomatik engeller
        await _redis.SetAddAsync(REDIS_KEY_SYMBOLS, symbol.ToUpper());
    }

    // Takip edilen tüm sembolleri getir
    public async Task<List<string>> GetActiveSymbolsAsync()
    {
        var symbols = await _redis.SetMembersAsync(REDIS_KEY_SYMBOLS);
        return symbols.Select(s => s.ToString()).ToList();
    }
}
