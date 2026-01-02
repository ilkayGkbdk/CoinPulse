using System;
using CoinPulse.Core.Interfaces;
using CoinPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoinPulse.Api.Jobs;

public class MarketReportingJob
{
    private readonly ILogger<MarketReportingJob> _logger;
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public MarketReportingJob(
        ILogger<MarketReportingJob> logger,
        AppDbContext dbContext,
        ICacheService cacheService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    // Görev 1: Dakikalık rapor
    public async Task GenerateDailyReportAsync()
    {
        // Örnek son 1 saatte en çok işlem gören coin
        // basitlik için sadece loglama yapıyoruz
        var count = await _dbContext.CryptoPrices.CountAsync();
        _logger.LogInformation($"[Hangfire 🚀] Dakikalık Rapor: Sistemde toplam {count} adet fiyat kaydı var.");
    }

    // Görev 2: Gece Temizliği (simülasyon)
    public async Task CleanupOldDataAsync()
    {
        _logger.LogWarning("[Hangfire 🧹] Veri temizliği başladı... (Simülasyon)");
        await Task.Delay(1000); // Sanki iş yapıyormuş gibi
        _logger.LogInformation("[Hangfire 🧹] Eski veriler arşivlendi.");
    }
}
