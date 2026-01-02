using System;
using CoinPulse.Core.Entities;
using CoinPulse.Core.Interfaces;
using Elastic.Clients.Elasticsearch;

namespace CoinPulse.Infrastructure.Search;

public class ElasticSearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "crypto_prices"; // Tablo adı gibi düşünülebilir

    public ElasticSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexPriceAsync(CryptoPrice price)
    {
        // Eğer tablo yoksa oluştur mantığı kurulabilir ama
        // elastic varsayılan olarak gönderdiğin veriye göre index oluşturur

        var response = await _client.IndexAsync(price, idx => idx.Index(IndexName));

        if (!response.IsValidResponse)
        {
            // Loglama yapılabilir
            throw new Exception("Elastic Index Hatası: " + response.DebugInformation);
        }
    }

    public async Task<IEnumerable<CryptoPrice>> SearchPricesAsync(string symbol, DateTime from, DateTime to)
    {
        var response = await _client.SearchAsync<CryptoPrice>(s => s
            .Index(IndexName) // Hangi indexte arama yapacağımız
            .Query(q => q
                .Bool(b => b
                    .Must(
                        // Sembol eşleşmesi
                        m => m.Term(t => t.Field(f => f.Symbol.Suffix("keyword")).Value(symbol)),
                        // Tarih aralığı
                        m => m.Range(r => r.DateRange(d => d.Field(f => f.Timestamp).Gte(from).Lte(to)))
                    )
                )
            )
            .Size(1000) // Maksimum 1000 sonuç döndür
            .Sort(srt => srt.Field(f => f.Timestamp, w => w.Order(SortOrder.Desc))) // yeniden eskiye sıralama
        );

        if (!response.IsValidResponse)
        {
            // Loglama yapılabilir
            return Enumerable.Empty<CryptoPrice>();
        }

        return response.Documents;
    }
}
