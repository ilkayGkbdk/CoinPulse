using CoinPulse.Api.Jobs;
using CoinPulse.Infrastructure;
using CoinPulse.Infrastructure.Logging;
using Hangfire;
using Hangfire.MemoryStorage;
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

    // --- HANGFIRE KURULUMU BAŞLANGIÇ ---
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage()); // RAM'de tutar

    // Hangfire Server (İşleri işleyen motor) API içinde çalışsın
    builder.Services.AddHangfireServer();
    // --- HANGFIRE KURULUMU BİTİŞ ---

    // --- HEALTH CHECKS SERVİSLERİ ---
    builder.Services.AddHealthChecks()
        // 1. SQLite Kontrolü
        .AddSqlite(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=../coinpulse.db",
            name: "SQLite DB 🗄️")

        // 2. Redis Kontrolü
        .AddRedis(
            builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379",
            name: "Redis Cache ⚡")

        // 3. RabbitMQ Kontrolü
        // --- DEĞİŞEN KISIM BAŞLANGIÇ ---
        // Eski AddRabbitMQ kütüphanesi sürüm uyumsuzluğu yaptığı için
        // doğrudan port kontrolü (TCP) yapıyoruz. Çok daha güvenlidir.
        .AddTcpHealthCheck(
            setup => setup.AddHost("localhost", 5672),
            name: "Message Queue 🐇")
        // --- DEĞİŞEN KISIM BİTİŞ ---

        // 4. Elasticsearch Kontrolü (URL'e ping atarak)
        .AddUrlGroup(
            new Uri("http://localhost:9200"),
            name: "Elasticsearch 🔎");

    // UI Servisi (Arayüz verilerini hafızada tutsun)
    builder.Services.AddHealthChecksUI(setup =>
    {
        setup.SetEvaluationTimeInSeconds(10); // 10 saniyede bir kontrol et
    })
    .AddSqliteStorage("Data Source=healthchecks.db");
    // -------------------------------

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

    // --- HANGFIRE DASHBOARD & JOBS ---
    // 1. Dashboard'u aktif et (/hangfire adresinde çalışır)
    app.UseHangfireDashboard();

    // 2. Periyodik İşleri Tanımla
    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

    // Her dakika çalışacak iş
    recurringJobManager.AddOrUpdate<MarketReportingJob>(
        "dakikalik-rapor",
        job => job.GenerateDailyReportAsync(),
        Cron.Minutely);

    // Her gün gece 03:00'te çalışacak iş
    recurringJobManager.AddOrUpdate<MarketReportingJob>(
        "gece-temizligi",
        job => job.CleanupOldDataAsync(),
        "0 3 * * *"); // Cron formatı
    // ---------------------------------

    // 2. Controller Rotalarını Eşlemelisin
    // (Gelen istekleri ilgili Controller'a yönlendirir)
    app.MapControllers();

    // Ham JSON verisi veren endpoint (DevOps araçları için)
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Görsel Panel (/health-ui)
    app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

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