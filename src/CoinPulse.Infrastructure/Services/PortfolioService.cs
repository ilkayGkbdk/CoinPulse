using System;
using CoinPulse.Core.DTOs;
using CoinPulse.Core.Entities;
using CoinPulse.Core.Interfaces;

namespace CoinPulse.Infrastructure.Services;

public class PortfolioService
{
    private readonly IGenericRepository<PortfolioTransaction> _transactionRepo;
    private readonly IGenericRepository<CryptoPrice> _priceRepo;
    private readonly ICacheService _cacheService;

    public PortfolioService(IGenericRepository<PortfolioTransaction> transactionRepo, IGenericRepository<CryptoPrice> priceRepo, ICacheService cacheService)
    {
        _transactionRepo = transactionRepo;
        _priceRepo = priceRepo;
        _cacheService = cacheService;
    }

    public async Task BuyAssetAsync(string userId, BuyCryptoDto dto)
    {
        decimal finalPrice;
        DateTime finalDate;

        // fiyat belirleme
        if (dto.Price.HasValue)
        {
            finalPrice = dto.Price.Value;
        }
        else
        {
            var symbolUpper = dto.Symbol.ToUpper();
            var cacheKey = $"price:{symbolUpper}";

            var cachedData = await _cacheService.GetAsync<CryptoPrice>(cacheKey);

            if (cachedData != null)
            {
                finalPrice = cachedData.Price;
            }
            else
            {
                var prices = await _priceRepo.GetAsync(x => x.Symbol == symbolUpper);
                var latestPrice = prices.OrderByDescending(x => x.DataTimestamp).FirstOrDefault();

                if (latestPrice == null)
                {
                    throw new Exception($"'{symbolUpper}' için fiyat verisi bulunamadı.");
                }

                finalPrice = latestPrice.Price;
            }
        }

        finalDate = dto.TransactionDate?.ToUniversalTime() ?? DateTime.UtcNow;

        var transaction = new PortfolioTransaction
        {
            UserId = userId,
            Symbol = dto.Symbol.ToUpper(),
            Amount = dto.Amount,
            BuyPrice = finalPrice,
            TransactionDate = finalDate
        };

        await _transactionRepo.AddAsync(transaction);
    }

    public async Task<List<PortfolioItemDto>> GetUserPortfolioAsync(string userId)
    {
        var transactions = await _transactionRepo.GetAsync(t => t.UserId == userId);

        var grouped = transactions
            .GroupBy(t => t.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TotalCost = g.Sum(t => t.TotalCost)
            })
            .ToList();

        var result = new List<PortfolioItemDto>();

        foreach (var item in grouped)
        {
            var cacheKey = $"price:{item.Symbol}";
            var cachedData = await _cacheService.GetAsync<CryptoPrice>(cacheKey);

            decimal currentPrice = cachedData?.Price ?? (item.TotalCost / (item.TotalAmount == 0 ? 1 : item.TotalAmount));

            var dto = new PortfolioItemDto
            {
                Symbol = item.Symbol,
                TotalAmount = item.TotalAmount,
                AverageCost = item.TotalAmount == 0 ? 0 : item.TotalCost / item.TotalAmount,
                CurrentPrice = currentPrice,
                CurrentValue = item.TotalAmount * currentPrice,
                ProfitLoss = (item.TotalAmount * currentPrice) - item.TotalCost
            };

            if (item.TotalCost > 0)
            {
                dto.ProfitLossPercentage = dto.ProfitLoss / item.TotalCost * 100;
            }

            result.Add(dto);
        }

        return result;
    }
}
