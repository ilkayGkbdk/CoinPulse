using CoinPulse.Core.Events;
using CoinPulse.Core.Interfaces;
using CoinPulse.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoinPulse.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PricesController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _dbContext;
        private readonly ISearchService _searchService;

        public PricesController(IPublishEndpoint publishEndpoint, ICacheService cacheService, AppDbContext dbContext, ISearchService searchService)
        {
            _publishEndpoint = publishEndpoint;
            _cacheService = cacheService;
            _dbContext = dbContext;
            _searchService = searchService;
        }

        [HttpPost]
        public async Task<IActionResult> PostPrice([FromBody] PriceRequest request)
        {
            // 1. Requesti evente dönüştür
            var priceEvent = new PriceUpdatedEvent
            {
                Symbol = request.Symbol,
                Price = request.Price,
                Timestamp = request.Timestamp ?? DateTime.UtcNow
            };

            // 2. Eventi RabbitMQ'ya yayınla
            await _publishEndpoint.Publish(priceEvent);

            return Accepted(new { status = "Queued", message = "Fiyat güncellemesi işleme alındı." });
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetLatestPrice(string symbol)
        {
            var cacheKey = $"price:{symbol.ToUpper()}";

            // Önce cahce'e bak
            var cachedPrice = await _cacheService.GetAsync<object>(cacheKey);
            if (cachedPrice != null)
            {
                return Ok(new { source = "Redis Cache", data = cachedPrice });
            }

            // Cache'de yoksa veritabanına bak
            var dbPrice = await _dbContext.CryptoPrices
                .Where(x => x.Symbol == symbol.ToUpper())
                .OrderByDescending(x => x.DataTimestamp)
                .FirstOrDefaultAsync();

            if (dbPrice == null)
            {
                return NotFound(new { message = "Belirtilen sembol için fiyat bulunamadı." });
            }

            // Veriyi cache'e ekle
            await _cacheService.SetAsync(cacheKey, dbPrice);

            return Ok(new { source = "SQLite DB", data = dbPrice });
        }

        // Geçmiş sorgulama
        [HttpGet("history/{symbol}")]
        public async Task<IActionResult> GetPriceHistory(string symbol, [FromQuery] int hours = 24)
        {
            var to = DateTime.UtcNow;
            var from = to.AddHours(-hours);

            // Bu sorgu SQLite'a değil, Elasticsearch'e gidiyor!
            var history = await _searchService.SearchPricesAsync(symbol.ToUpper(), from, to);

            return Ok(new
            {
                source = "Elasticsearch",
                count = history.Count(),
                data = history
            });
        }
    }

    public record PriceRequest(string Symbol, decimal Price, DateTime? Timestamp);
}
