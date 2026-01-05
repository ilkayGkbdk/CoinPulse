using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoinPulse.Core.Events;
using CoinPulse.Infrastructure.Services;
using MassTransit;

namespace CoinPulse.Worker.Services;

public class BinanceIngestionWorker : BackgroundService
{
    private readonly ILogger<BinanceIngestionWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IServiceProvider _serviceProvider; // Scoped servis çağırmak için

    public BinanceIngestionWorker(
        ILogger<BinanceIngestionWorker> logger,
        IHttpClientFactory httpClientFactory,
        IPublishEndpoint publishEndpoint,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _publishEndpoint = publishEndpoint;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk açılışta varsayılanları yükle
        using (var scope = _serviceProvider.CreateScope())
        {
            var symbolService = scope.ServiceProvider.GetRequiredService<SymbolService>();
            await symbolService.InitializeDefaultsAsync();
        }

        _logger.LogInformation("🌍 Dinamik Veri Akışı Başlatılıyor...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndPublishPrices();
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veri döngüsü hatası!");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task FetchAndPublishPrices()
    {
        using var scope = _serviceProvider.CreateScope();
        var symbolService = scope.ServiceProvider.GetRequiredService<SymbolService>();

        // Redis'ten güncel listeyi çek
        var activeSymbols = await symbolService.GetActiveSymbolsAsync();

        using var client = _httpClientFactory.CreateClient();

        // Binance her seferinde tek tek sormak yerine toplu fiyat sorabiliriz (Optimasyon)
        // Ama basitlik için döngüyle devam edelim.
        foreach (var symbol in activeSymbols)
        {
            // Binance'de Gümüş (XAG) ve Altın (XAU) genelde PAXG veya farklı paritelerdedir.
            // Basitlik için hepsine USDT ekleyip soruyoruz.
            var binanceSymbol = $"{symbol}USDT";

            // NOT: Binance'de her sembol USDT ile bitmez (Örn: BTCTRY). 
            // İleride mapping tablosu yapılabilir.

            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={binanceSymbol}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<BinanceTicker>(content);

                if (data != null && decimal.TryParse(data.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                {
                    await _publishEndpoint.Publish(new PriceUpdatedEvent
                    {
                        Symbol = symbol, // Orijinal sembolü kullan (BTC)
                        Price = price,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            else
            {
                // Binance'de yoksa logla (Kullanıcı saçma bir şey girdiyse)
                // _logger.LogWarning($"Binance'de bulunamadı: {binanceSymbol}");
            }
        }
    }
}

// Binance API Yanıt Modeli
public class BinanceTicker
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty; // Binance fiyatı string döner
}