using System;
using CoinPulse.Core.Interfaces;
using CoinPulse.Infrastructure.Caching;
using CoinPulse.Infrastructure.Data;
using CoinPulse.Infrastructure.Search;
using Elastic.Clients.Elasticsearch;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CoinPulse.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. SQLite (Mevcut)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=../coinpulse.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        // 2. Redis Bağlantısı (YENİ)
        // Docker'da redis servisi 'localhost:6379' adresindedir.
        var redisConfig = configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379";

        // Redis bağlantısını Singleton olarak ekleriz (Tüm uygulama ömrünce tek bağlantı)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConfig));

        // Bizim yazdığımız Cache servisi
        services.AddScoped<ICacheService, RedisCacheService>();

        // 3. Elasticsearch (YENİ)
        var elasticUri = configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";

        var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
            .DefaultIndex("crypto-prices")
            // Docker dev ortamında SSL sertifikası (HTTPS) kullanmıyoruz, o yüzden disable ediyoruz:
            .ServerCertificateValidationCallback((o, certificate, chain, errors) => true);

        var client = new ElasticsearchClient(settings);

        services.AddSingleton(client);
        services.AddScoped<ISearchService, ElasticSearchService>();

        return services;
    }
}
