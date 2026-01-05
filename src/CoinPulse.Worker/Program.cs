using CoinPulse.Infrastructure.Extensions; // Extensions
using CoinPulse.Infrastructure.Logging;
using CoinPulse.Worker.Consumers;
using CoinPulse.Worker.Services;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Worker");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddHttpClient();

    // 1. Altyapı (DB, Redis, Elastic, Repository)
    // Worker sadece bunları kullanır, Hangfire veya HealthUI sunmaz.
    builder.Services.AddInfrastructure(builder.Configuration);

    // YENİ: Binance Robotunu Başlat
    // (Not: Bunu istersen bir Config bayrağına bağlayabilirsin, localde çok trafik yapmasın diye)
    builder.Services.AddHostedService<BinanceIngestionWorker>();

    // 2. Mesajlaşma (RabbitMQ) - Kendi Consumer'ını veriyoruz
    builder.Services.AddMessageBroker<PriceUpdatedConsumer>(builder.Configuration);

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker servisi çöktü!");
}
finally
{
    Log.CloseAndFlush();
}