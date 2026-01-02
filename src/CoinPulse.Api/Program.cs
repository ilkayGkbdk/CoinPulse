using CoinPulse.Infrastructure;
using CoinPulse.Infrastructure.Logging;
using MassTransit;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Api");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // 1. Controller Servislerini Eklemelisin
    // (Yoksa PricesController sınıfını sistem görmez)
    builder.Services.AddControllers();

    // Add services to the container.
    builder.Services.AddOpenApi();

    // Bizim yazdığımız altyapı servisi (DB + RabbitMQ)
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // API sadece mesaj gönderir (Producer), bu yüzden Consumer tanımlamıyoruz.
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Opsiyonel: Eğer Swagger UI görmek istersen buraya Scalar veya SwaggerUI eklenebilir
        // ama şimdilik /openapi/v1.json adresinden şemayı görebilirsin.
    }

    app.UseHttpsRedirection();

    app.UseAuthorization(); // Genelde standartta bulunur, kalsın.

    // 2. Controller Rotalarını Eşlemelisin
    // (Gelen istekleri ilgili Controller'a yönlendirir)
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmedik bir şekilde sonlandı!");
}
finally
{
    Log.CloseAndFlush();
}