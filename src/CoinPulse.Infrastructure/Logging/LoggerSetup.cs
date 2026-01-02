using System;
using Serilog;
using Serilog.Events;

namespace CoinPulse.Infrastructure.Logging;

public static class LoggerSetup
{
    public static void ConfigureLogging(string appName)
    {
        Log.Logger = new LoggerConfiguration()
            // 1. Minimum Log Seviyesi (Information ve üzerini al)
            .MinimumLevel.Information()
            // Microsoft ve System loglarını biraz kısıyoruz, sadece Warning (Uyarı) verirse yazsın
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information) // EF Core sorgularını görmek için

            // 2. Logların Çıktı Yerleri (Sinks)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName) // Logun hangi projeden geldiğini yazar

            // Konsola renkli yaz
            .WriteTo.Console()

            // Dosyaya yaz (logs klasörü altına, gün bazlı)
            .WriteTo.File(
                path: $"logs/{appName}-.txt", // Örn: logs/Api-20251231.txt
                rollingInterval: RollingInterval.Day, // Her gün yeni dosya aç
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
    }
}
