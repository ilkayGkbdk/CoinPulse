using System;
using CoinPulse.Core.Entities;
using CoinPulse.Core.Interfaces;
using Elastic.Clients.Elasticsearch;

namespace CoinPulse.Infrastructure.Search;

public class ElasticSearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "crypto-prices";

    public ElasticSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexPriceAsync(CryptoPrice price)
    {
        var response = await _client.IndexAsync(price, idx => idx.Index(IndexName));
        if (!response.IsValidResponse)
        {
            throw new Exception($"Elastic Index Hatası: {response.DebugInformation}");
        }
    }

    public async Task<IEnumerable<CryptoPrice>> SearchPricesAsync(string symbol, DateTime from, DateTime to)
    {
        var response = await _client.SearchAsync<CryptoPrice>(s => s
            .Index(IndexName)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        // Sembol için keyword (Küçük harf hassasiyeti)
                        // Elastic genelde camelCase kaydeder: "symbol"
                        m => m.Term(t => t.Field("symbol.keyword").Value(symbol)),

                        // Tarih aralığı
                        // Property adı DataTimestamp olsa da Elastic'te "dataTimestamp" diye geçer.
                        // C# Expression kullanmak yerine string olarak alan adını veriyoruz.
                        m => m.Range(r => r.DateRange(d => d.Field("dataTimestamp").Gte(from).Lte(to)))
                    )
                )
            )
            .Sort(srt => srt.Field("dataTimestamp", w => w.Order(SortOrder.Desc)))
            .Size(1000)
        );

        if (!response.IsValidResponse)
        {
            // Hata detayını görmek için loglayabilirsin
            // Console.WriteLine(response.DebugInformation);
            return Enumerable.Empty<CryptoPrice>();
        }

        return response.Documents;
    }
}
