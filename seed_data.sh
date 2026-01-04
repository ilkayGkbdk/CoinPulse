#!/bin/bash

# API Adresini kontrol et!
URL="http://localhost:5089/api/prices"

# Coin listesi
COINS=("BTC" "ETH" "SOL" "AVAX" "DOGE" "XRP" "BNB" "ADA")

echo "🚀 Piyasa simülasyonu (Smart Timestamp) başlıyor..."

for i in {1..100}
do
    RANDOM_INDEX=$((RANDOM % ${#COINS[@]}))
    SYMBOL=${COINS[$RANDOM_INDEX]}

    # Fiyat Üretimi
    if [ "$SYMBOL" == "BTC" ]; then PRICE=$((95000 + RANDOM % 2000)).$((RANDOM % 99));
    elif [ "$SYMBOL" == "ETH" ]; then PRICE=$((3400 + RANDOM % 200)).$((RANDOM % 99));
    elif [ "$SYMBOL" == "SOL" ]; then PRICE=$((140 + RANDOM % 10)).$((RANDOM % 99));
    else PRICE=$((1 + RANDOM % 100)).$((RANDOM % 99)); fi
    
    # TIMESTAMP (ISO 8601 UTC Formatı)
    # date -u +"%Y-%m-%dT%H:%M:%SZ" komutu UTC zaman verir.
    # macOS'ta 'date' komutu biraz farklı çalışabilir, en garantisi:
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    # API'ye timestamp alanı ile gönderiyoruz
    # Not: API'deki 'PriceRequest' nesnesine bu alanı eklememiz gerekecek!
    curl -s -o /dev/null -X POST "$URL" \
       -H "Content-Type: application/json" \
       -d "{\"symbol\": \"$SYMBOL\", \"price\": $PRICE, \"timestamp\": \"$TIMESTAMP\"}"

    echo "[$i] $SYMBOL ($PRICE $) @ $TIMESTAMP gönderildi."
    sleep 0.2
done

echo "✅ Veri yükleme tamamlandı!"
```

---

### 3. Backend Tarafını Buna Uyarlama

Script artık timestamp gönderiyor ama bizim Backend (`PriceRequest`) bunu karşılamıyor. Hemen güncelleyelim.

**Adım 1: API DTO Güncellemesi**
`src/CoinPulse.Api/Controllers/PricesController.cs` dosyasının en altındaki `PriceRequest` kaydını güncelle:

```csharp
// timestamp opsiyonel olabilir, gönderilmezse UtcNow kullanılır.
public record PriceRequest(string Symbol, decimal Price, DateTime? Timestamp);
```

**Adım 2: Controller Mantığı**
Yine `PricesController.cs` içindeki `PostPrice` metodunu güncelle:

```csharp
[HttpPost]
public async Task<IActionResult> PostPrice([FromBody] PriceRequest request)
{
    var priceEvent = new PriceUpdatedEvent
    {
        Symbol = request.Symbol,
        Price = request.Price,
        // Eğer scriptten tarih gelirse onu kullan, gelmezse şu anı al.
        Timestamp = request.Timestamp ?? DateTime.UtcNow 
    };

    await _publishEndpoint.Publish(priceEvent);
    return Accepted(new { status = "Queued", message = "Fiyat işleme alındı." });
}