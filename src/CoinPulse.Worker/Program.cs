using CoinPulse.Infrastructure;
using CoinPulse.Infrastructure.Logging;
using CoinPulse.Worker;
using CoinPulse.Worker.Consumers;
using MassTransit;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Worker");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders(); // Varsayılan logları temizle
    builder.Logging.AddSerilog();     // Serilog'u ekle

    builder.Services.AddHostedService<Worker>();

    // 1. Veritabanı erişimi için Infrastructure servisini ekle
    // Worker da DB'ye yazacağı için buna ihtiyacı var.
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // 2. MassTransit ve Consumer Yapılandırması
    builder.Services.AddMassTransit(x =>
    {
        // Consumer sınıfımızı sisteme tanıtıyoruz
        x.AddConsumer<PriceUpdatedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            // Bu komut, PriceUpdatedConsumer için otomatik bir kuyruk (Queue) oluşturur.
            cfg.ConfigureEndpoints(context);
        });
    });

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