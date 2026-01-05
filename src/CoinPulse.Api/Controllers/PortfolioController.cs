using System.Security.Claims;
using CoinPulse.Core.DTOs;
using CoinPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoinPulse.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly PortfolioService _portfolioService;

        public PortfolioController(PortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        // POST: api/Portfolio/buy
        [HttpPost("buy")]
        public async Task<IActionResult> BuyCrypto([FromBody] BuyCryptoDto dto)
        {
            // token içinden userId al
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Kullanıcı kimliği alınamadı.");

            await _portfolioService.BuyAssetAsync(userId, dto);

            return Ok(new { message = $"{dto.Amount} adet {dto.Symbol} portföye eklendi." });
        }

        // GET: api/portfolio
        [HttpGet]
        public async Task<IActionResult> GetMyPortfolio()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var portfolio = await _portfolioService.GetUserPortfolioAsync(userId);

            return Ok(new { source = "Calculated Live 💰", data = portfolio });
        }
    }
}
