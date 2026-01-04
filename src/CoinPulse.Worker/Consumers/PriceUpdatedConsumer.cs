using System;
using CoinPulse.Core.Entities;
using CoinPulse.Core.Events;
using CoinPulse.Core.Interfaces;
using CoinPulse.Infrastructure.Data;
using MassTransit;

namespace CoinPulse.Worker.Consumers;

public class PriceUpdatedConsumer : IConsumer<PriceUpdatedEvent>
{
    private readonly ILogger<PriceUpdatedConsumer> _logger;
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ISearchService _searchService;

    public PriceUpdatedConsumer(ILogger<PriceUpdatedConsumer> logger, AppDbContext dbContext, ICacheService cacheService, ISearchService searchService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cacheService = cacheService;
        _searchService = searchService;
    }

    public async Task Consume(ConsumeContext<PriceUpdatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation($"[RabbitMQ] Mesaj Yakalandı: {message.Symbol} - {message.Price} at {message.Timestamp}");

        // Fiyat güncellemesini veritabanına kaydet
        var entity = new CryptoPrice
        {
            Symbol = message.Symbol,
            Price = message.Price,
            // Timestamp -> DataTimestamp oldu
            DataTimestamp = message.Timestamp,
            // CreatedAt otomatik dolacak (BaseEntity)
        };

        _dbContext.CryptoPrices.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"[SQLite] Veri başarıyla kaydedildi. ID: {entity.Id}");

        // Redis güncellemesi
        // Key formatı: "price:BTC", "price:ETH" vb.
        var cacheKey = $"price:{message.Symbol.ToUpper()}";

        // Sadece fiyat yerine tüm nesneyi cache'e kaydet
        await _cacheService.SetAsync(cacheKey, entity, TimeSpan.FromHours(1));

        _logger.LogInformation($"[Redis] Cache güncellendi: {cacheKey} -> {message.Price}$");

        // Elasticsearch indexleme
        // Loglama/analiz için veriyi elastic'e gönder
        // buradaki tüm akışı bozmasın diye try-catch ile sarmalayabiliriz
        try
        {
            await _searchService.IndexPriceAsync(entity);
            _logger.LogInformation($"[Elastic] Veri indekslendi: {message.Symbol}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Elastic] Veri indeksleme hatası: " + ex.Message);
        }
    }
}
