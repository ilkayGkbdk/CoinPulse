#!/bin/bash

# API Adresini buraya yaz (Kendi portunu kontrol et!)
URL="http://localhost:5089/api/prices"

# Coin listesi
COINS=("BTC" "ETH" "SOL" "AVAX" "DOGE" "XRP" "BNB" "ADA")

echo "🚀 Piyasa simülasyonu başlıyor... (Durdurmak için CTRL+C)"

# Sonsuz döngü (veya for i in {1..100} yaparak 100 tane atabilirsin)
for i in {1..50}
do
    # 1. Rastgele Coin Seç
    RANDOM_INDEX=$((RANDOM % ${#COINS[@]}))
    SYMBOL=${COINS[$RANDOM_INDEX]}

    # 2. Rastgele Fiyat Üret (Coin'e göre mantıklı fiyatlar)
    if [ "$SYMBOL" == "BTC" ]; then
        PRICE=$((95000 + RANDOM % 2000)).$((RANDOM % 99))
    elif [ "$SYMBOL" == "ETH" ]; then
        PRICE=$((3400 + RANDOM % 200)).$((RANDOM % 99))
    elif [ "$SYMBOL" == "SOL" ]; then
        PRICE=$((140 + RANDOM % 10)).$((RANDOM % 99))
    else
        PRICE=$((1 + RANDOM % 100)).$((RANDOM % 99))
    fi

    # 3. API'ye İstek At (Sessiz modda)
    curl -s -o /dev/null -X POST "$URL" \
       -H "Content-Type: application/json" \
       -d "{\"symbol\": \"$SYMBOL\", \"price\": $PRICE}"

    echo "[$i] $SYMBOL fiyatı güncellendi: $PRICE $"
    
    # Çok hızlı olmasın, biraz bekle (0.2 saniye)
    sleep 0.2
done

echo "✅ Veri yükleme tamamlandı!"
