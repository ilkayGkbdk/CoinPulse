using CoinPulse.Api.Consumers;
using CoinPulse.Api.Hubs;
using CoinPulse.Api.Jobs;
using CoinPulse.Infrastructure.Data;
using CoinPulse.Infrastructure.Extensions; // Yeni extensions burada
using CoinPulse.Infrastructure.Logging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

LoggerSetup.ConfigureLogging("CoinPulse.Api");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CoinPulse API",
            Version = "v1",
            Description = "Cryptocurrency portfolio management API"
        });
    });
    builder.Services.AddSignalR();

    // --- TEMİZLENMİŞ SERVİS KAYITLARI ---
    // 1. Altyapı (DB, Redis, Elastic, Repository)
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- YENİ EKLENEN ---
    builder.Services.AddIdentityServices(builder.Configuration);

    // 2. Mesajlaşma (RabbitMQ) - Buraya kendi Consumer'ını veriyoruz
    builder.Services.AddMessageBroker<PriceNotificationConsumer>(builder.Configuration);

    // 3. Arka Plan İşleri (Hangfire)
    builder.Services.AddBackgroundJobs();

    // 4. İzleme (HealthChecks)
    builder.Services.AddMonitoring(builder.Configuration);
    // ------------------------------------

    var app = builder.Build();

    // Otomatik Migration
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.UseSerilogRequestLogging();
    app.UseCors(x => x.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoinPulse API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Dashboardlar
    app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireAuthorizationFilter() } });
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

    app.MapHub<CryptoHub>("/hubs/crypto");
    app.MapControllers();

    // Jobs
    var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    jobManager.AddOrUpdate<MarketReportingJob>("dakikalik-rapor", job => job.GenerateDailyReportAsync(), Cron.Minutely);
    jobManager.AddOrUpdate<MarketReportingJob>("gece-temizligi", job => job.CleanupOldDataAsync(), "0 3 * * *");

    app.Run();
}
catch (Exception ex)
{
    // Bu hata EF Core araçları çalışırken kasten fırlatılır, loglamaya gerek yok.
    if (ex.GetType().Name == "HostAbortedException")
    {
        return;
    }

    Log.Fatal(ex, "Uygulama beklenmedik şekilde sonlandı!");
}
finally
{
    Log.CloseAndFlush();
}

// Filtre sınıfı aşağıda kalabilir veya ayrı dosyaya taşınabilir
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}