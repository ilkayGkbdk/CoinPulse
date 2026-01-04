using CoinPulse.Api.Consumers;
using CoinPulse.Api.Hubs;
using CoinPulse.Api.Jobs;
using CoinPulse.Infrastructure;
using CoinPulse.Infrastructure.Data;
using CoinPulse.Infrastructure.Logging;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Api");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddSignalR();

    // --- DEĞİŞKENLERİ ALALIM (Düzeltme Burada) ---
    // Docker'dan "rabbitmq" gelecek, Local'de "localhost" kalacak.
    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var elasticUri = builder.Configuration["ElasticSearch:Uri"] ?? "http://localhost:9200";
    // ---------------------------------------------

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage());
    builder.Services.AddHangfireServer();

    // --- HEALTH CHECKS (Düzeltildi) ---
    builder.Services.AddHealthChecks()
        .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=../coinpulse.db", name: "SQLite DB 🗄️")
        .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379", name: "Redis Cache ⚡")
        // Hardcoded "localhost" YERİNE değişkenden gelen "rabbitHost" kullanıyoruz:
        .AddTcpHealthCheck(setup => setup.AddHost(rabbitHost, 5672), name: "Message Queue 🐇")
        // Hardcoded URL YERİNE değişkenden gelen "elasticUri" kullanıyoruz:
        .AddUrlGroup(new Uri(elasticUri), name: "Elasticsearch 🔎");

    builder.Services.AddHealthChecksUI(setup => { setup.SetEvaluationTimeInSeconds(10); })
        .AddSqliteStorage("Data Source=healthchecks.db");

    // --- MASSTRANSIT (Düzeltildi) ---
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<PriceNotificationConsumer>();
        x.UsingRabbitMq((context, cfg) =>
        {
            // Hardcoded "localhost" YERİNE değişkenden gelen "rabbitHost" kullanıyoruz:
            cfg.Host(rabbitHost, "/", h => { h.Username("guest"); h.Password("guest"); });
            cfg.ConfigureEndpoints(context);
        });
    });

    var app = builder.Build();

    // --- OTOMATİK MIGRATION (YENİ) ---
    // Uygulama başlarken DB yoksa oluşturur ve tabloları ekler.
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            // Eğer veritabanı yoksa oluştur, varsa eksik migrationları uygula
            dbContext.Database.Migrate();
            Log.Information("✅ Veritabanı başarıyla güncellendi (Migrated).");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Veritabanı migration hatası!");
        }
    }
    // ----------------------------------

    app.UseSerilogRequestLogging();

    app.UseCors(x => x.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(origin => true).AllowCredentials());

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
    app.MapHub<CryptoHub>("/hubs/crypto");
    app.MapControllers();

    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<MarketReportingJob>("dakikalik-rapor", job => job.GenerateDailyReportAsync(), Cron.Minutely);
    recurringJobManager.AddOrUpdate<MarketReportingJob>("gece-temizligi", job => job.CleanupOldDataAsync(), "0 3 * * *");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmedik şekilde sonlandı!");
}
finally
{
    Log.CloseAndFlush();
}

// Hangfire Dashboard'a girişe izin veren filtre
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Production'da buraya şifre/kullanıcı kontrolü konur.
        // Dev ortamı için herkese izin veriyoruz (True).
        return true;
    }
}