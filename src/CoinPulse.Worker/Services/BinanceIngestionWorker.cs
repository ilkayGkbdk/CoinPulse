using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoinPulse.Core.Events;
using MassTransit;

namespace CoinPulse.Worker.Services;

public class BinanceIngestionWorker : BackgroundService
{
    private readonly ILogger<BinanceIngestionWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPublishEndpoint _publishEndpoint;

    private readonly List<string> _symbols = new() { "BTCUSDT", "ETHUSDT", "SOLUSDT", "AVAXUSDT", "XRPUSDT" };

    public BinanceIngestionWorker(ILogger<BinanceIngestionWorker> logger, IHttpClientFactory httpClientFactory, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _publishEndpoint = publishEndpoint;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🌍 Binance Veri Akışı Başlatılıyor...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Her 5 saniyede bir çalış
                await FetchAndPublishPrices();
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Binance veri çekme hatası!");
                await Task.Delay(10000, stoppingToken); // Hata varsa 10sn bekle
            }
        }
    }

    private async Task FetchAndPublishPrices()
    {
        using var client = _httpClientFactory.CreateClient();

        foreach (var symbol in _symbols)
        {
            // Binance Public API (Auth gerektirmez)
            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<BinanceTicker>(content);

                if (data != null && decimal.TryParse(data.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                {
                    // "BTCUSDT" -> "BTC" yapalım (Sondaki USDT'yi at)
                    var cleanSymbol = symbol.Replace("USDT", "");

                    // RabbitMQ'ya fırlat! (Bizim API'deki PostPrice metodunun yaptığı işi yapıyor)
                    await _publishEndpoint.Publish(new PriceUpdatedEvent
                    {
                        Symbol = cleanSymbol,
                        Price = price,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"✅ Binance: {cleanSymbol} -> {price}$");
                }
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