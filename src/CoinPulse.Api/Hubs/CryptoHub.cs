using System;
using Microsoft.AspNetCore.SignalR;

namespace CoinPulse.Api.Hubs;

/// <summary>
/// Frontend'in bağlandığı hub.
/// Şimdilik boş. Sadece dışarıya açmak için var.
/// </summary>
public class CryptoHub : Hub
{
    /// <summary>
    /// Yeni bir istemci bağlandığında çağrılır.
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Yeni bir istemci bağlandı: {Context.ConnectionId}");
    }
}
