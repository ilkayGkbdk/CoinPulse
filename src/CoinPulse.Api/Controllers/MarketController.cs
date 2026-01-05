using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoinPulse.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MarketController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchSymbols(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Ok(new List<string>());

            using var client = _httpClientFactory.CreateClient();

            // Binance Exchange Info (Tüm sembolleri verir, biraz ağırdır o yüzden cachelenebilir)
            // Basitlik için direkt çağırıyoruz.
            var response = await client.GetAsync("https://api.binance.com/api/v3/exchangeInfo");

            if (!response.IsSuccessStatusCode) return BadRequest("Borsa verisine ulaşılamadı.");

            var content = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<BinanceExchangeInfo>(content);

            var q = query.ToUpper();

            // Sadece USDT paritelerini filtrele ve kullanıcıya sun
            var matches = info?.Symbols
                .Where(s => s.SymbolName.EndsWith("USDT") && s.SymbolName.Contains(q))
                .Select(s => s.SymbolName.Replace("USDT", "")) // Kullanıcıya sadece "BTC" göster
                .Take(10) // En fazla 10 sonuç
                .ToList();

            return Ok(matches);
        }
    }

    // Binance Modelleri
    public class BinanceExchangeInfo
    {
        [JsonPropertyName("symbols")]
        public List<BinanceSymbol> Symbols { get; set; } = new();
    }

    public class BinanceSymbol
    {
        [JsonPropertyName("symbol")]
        public string SymbolName { get; set; } = string.Empty;
    }
}
