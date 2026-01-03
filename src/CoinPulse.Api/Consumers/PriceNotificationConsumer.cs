using System;
using CoinPulse.Api.Hubs;
using CoinPulse.Core.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace CoinPulse.Api.Consumers;

public class PriceNotificationConsumer : IConsumer<PriceUpdatedEvent>
{
    private readonly IHubContext<CryptoHub> _hubContext;
    private readonly ILogger<PriceNotificationConsumer> _logger;

    public PriceNotificationConsumer(IHubContext<CryptoHub> hubContext, ILogger<PriceNotificationConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PriceUpdatedEvent> context)
    {
        var message = context.Message;

        // Signalr ile bağlantılı tüm clientlara fiyat güncellemesini gönder
        // Metod adı: "ReceivePriceUpdate" (frontend tarafında bu isimle dinlenecek)
        await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new
        {
            symbol = message.Symbol,
            price = message.Price,
            timestamp = message.Timestamp
        });

        _logger.LogInformation($"[SignalR 📡] Fiyat clientlara basıldı: {message.Symbol}");
    }
}
