using CoinPulse.Infrastructure;
using CoinPulse.Infrastructure.Logging;
using CoinPulse.Worker.Consumers;
using MassTransit;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Worker");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddInfrastructureServices(builder.Configuration);

    // --- DEĞİŞKENLERİ ALALIM ---
    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    // ---------------------------

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<PriceUpdatedConsumer>();
        x.UsingRabbitMq((context, cfg) =>
        {
            // Hardcoded "localhost" YERİNE "rabbitHost"
            cfg.Host(rabbitHost, "/", h => { h.Username("guest"); h.Password("guest"); });
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