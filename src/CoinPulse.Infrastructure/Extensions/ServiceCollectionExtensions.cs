using System;
using CoinPulse.Core.Interfaces;
using CoinPulse.Infrastructure.Caching;
using CoinPulse.Infrastructure.Data;
using CoinPulse.Infrastructure.Repositories;
using CoinPulse.Infrastructure.Search;
using Elastic.Clients.Elasticsearch;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CoinPulse.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Veritabanı (PostgreSQL)
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // UseSqlite YERİNE UseNpgsql
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Generic Repository
        // (Scoped: Her HTTP isteği için yeni bir tane oluşturur)
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // 3. Redis Cache
        var redisConfig = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConfig));
        services.AddScoped<ICacheService, RedisCacheService>();

        // 4. Elasticsearch
        var elasticUri = configuration["ElasticSearch:Uri"] ?? "http://localhost:9200";
        var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
            .DefaultIndex("crypto-prices")
            .ServerCertificateValidationCallback((o, certificate, chain, errors) => true);
        var client = new ElasticsearchClient(settings);
        services.AddSingleton(client);
        services.AddScoped<ISearchService, ElasticSearchService>();

        return services;
    }

    // Hangfire Ayarları
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage());

        services.AddHangfireServer();
        return services;
    }

    // MassTransit (RabbitMQ) Ayarları
    // TConsumer: API ve Worker farklı consumer'lar kullanacağı için generic yaptık.
    public static IServiceCollection AddMessageBroker<TConsumer>(this IServiceCollection services, IConfiguration configuration)
        where TConsumer : class, IConsumer
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";

        services.AddMassTransit(x =>
        {
            // Parametre olarak gelen Consumer'ı ekle
            x.AddConsumer<TConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h => { h.Username("guest"); h.Password("guest"); });
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    // HealthChecks Ayarları
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var elasticUri = configuration["ElasticSearch:Uri"] ?? "http://localhost:9200";
        var redisConfig = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var dbString = configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks()
            .AddNpgSql(dbString!, name: "PostgreSQL DB 🐘")
            .AddRedis(redisConfig, name: "Redis Cache ⚡")
            .AddTcpHealthCheck(setup => setup.AddHost(rabbitHost, 5672), name: "Message Queue 🐇")
            .AddUrlGroup(new Uri(elasticUri), name: "Elasticsearch 🔎");

        services.AddHealthChecksUI(setup => setup.SetEvaluationTimeInSeconds(10))
        .AddPostgreSqlStorage(dbString!, options =>
        {
            // .NET 9/10 ile gelen katı migration kontrolünü bu context için kapatıyoruz.
            // Çünkü bu context bizim değil, kütüphanenin ve migration ekleyemeyiz.
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
