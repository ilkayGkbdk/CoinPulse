# 🚀 CoinPulse - Real-Time Crypto Analysis Engine

![CoinPulse Banner](https://via.placeholder.com/1200x300.png?text=CoinPulse+High+Performance+Architecture)

**CoinPulse**, yüksek trafikli finansal veri akışlarını (High Frequency Data) işleyen, analiz eden ve raporlayan **Modüler Monolitik** yapıda geliştirilmiş bir .NET çözümüdür.

Bu proje; mikroservis mimarisine geçişe hazır, ölçeklenebilir ve hataya dayanıklı (resilient) bir backend mimarisini simüle etmek amacıyla geliştirilmiştir. Modern yazılım mühendisliği prensipleri (**Clean Architecture**, **Distributed Locking**, **Async Messaging**, **Event-Driven**) kullanılarak **Mid/Senior Level** teknik yetkinlikleri sergilemek üzere tasarlanmıştır.

---

## 🏗 Mimari ve Teknoloji Yığını

Proje, **Clean Architecture** prensiplerine sadık kalınarak katmanlı bir yapıda (`Api`, `Core`, `Infrastructure`, `Worker`) tasarlanmıştır.

| Teknoloji | Sürüm | Kullanım Amacı |
|-----------|-------|----------------|
| **.NET** | 10 (Preview) | Ana geliştirme platformu (API & Worker servisleri). |
| **SQLite** | - | İlişkisel veriler, kullanıcı profilleri ve yapılandırma ayarları. |
| **RabbitMQ** | 3.x | Yüksek trafikli veri girişini (Ingestion) karşılayan asenkron mesaj kuyruğu. |
| **Redis** | Alpine | Anlık fiyat verileri için **Cache** ve veri tutarlılığı için **Distributed Lock**. |
| **Elasticsearch** | 8.17 | Zaman serisi (Time-Series) verilerinin saklanması ve karmaşık raporlama sorguları. |
| **Hangfire** | 1.8 | Periyodik raporlama, veri temizliği ve zamanlanmış arka plan işleri (Cron Jobs). |
| **Serilog** | - | Merkezi ve yapısal (Structured) loglama altyapısı. |
| **HealthChecks** | - | Tüm dış bağımlılıkların (DB, Queue, Cache) anlık durum takibi ve görsel dashboard. |

---

## ⚡ Temel Özellikler

### 1. 📨 Asenkron Veri İşleme (Fire-and-Forget)
Kullanıcıdan gelen binlerce anlık fiyat isteği, API katmanında bekletilmeden **RabbitMQ** kuyruğuna iletilir (`IPublishEndpoint`). `Worker` servisi arka planda bu kuyruğu tüketerek (Consumer) veritabanına yazar.

* **Fayda:** API yanıt süresi milisaniyeler seviyesindedir ve ani trafik artışlarında (Traffic Spike) sistem kilitlenmez.

### 2. 🔒 Cache-Aside & Distributed Locking
Sık erişilen "Son Fiyat" verileri veritabanı yerine **Redis** üzerinden sunulur. Eş zamanlı güncellemelerde veri tutarlılığını sağlamak ve "Race Condition" durumlarını önlemek amacıyla Redis tabanlı dağıtık kilit (Distributed Lock) mekanizması uygulanmıştır.

### 3. 📊 Analytics & Reporting
SQLite ilişkisel veriler için optimize edilmiştir ancak büyük veri analizinde yetersiz kalabilir. CoinPulse, raporlama sorgularını (örn: *"Son 24 saatteki en volatil coin"*, *"XRP Fiyat Geçmişi"*) **Elasticsearch** üzerinden saniyeler içinde yanıtlar.

### 4. 🏥 Self-Healing & Monitoring
Sistem kendi sağlığını sürekli izler. `/health-ui` adresinden RabbitMQ kuyruğunun erişilebilirliği, Redis'in yanıt süresi, Disk durumu ve Elasticsearch bağlantısı görsel olarak takip edilir. Herhangi bir servis çöktüğünde UI üzerinde anında kırmızı alarm verilir.

---

## 🛠 Kurulum ve Çalıştırma

Gereksinimler: **Docker Desktop** ve **.NET SDK 10**.

### 1. Altyapıyı Başlatın (Docker)

Proje kök dizininde aşağıdaki komutu çalıştırarak RabbitMQ, Redis, Elasticsearch ve Kibana'yı ayağa kaldırın.

```bash
docker compose up -d
```

Kontrol etmek için:

```bash
docker compose ps
```

### 2. Veritabanını Hazırlayın

Entity Framework Core migration'larını çalıştırarak SQLite veritabanını oluşturun.

```bash
dotnet ef database update -p src/CoinPulse.Infrastructure -s src/CoinPulse.Api
```

### 3. Servisleri Başlatın

Sistemi tam simüle etmek için iki ayrı terminalde API ve Worker projelerini çalıştırın.

**Terminal 1 (API - Sunum Katmanı):**

```bash
dotnet run --project src/CoinPulse.Api
```

**Terminal 2 (Worker - İşleyen Katman):**

```bash
dotnet run --project src/CoinPulse.Worker
```

---

## 🖥 Dashboard ve Arayüzler

Uygulama ayağa kalktığında aşağıdaki adreslerden yönetim panellerine erişebilirsiniz:

| Arayüz | URL | Açıklama |
|--------|-----|----------|
| **API Dokümantasyonu** | `http://localhost:5089/openapi/v1.json` | OpenAPI/Swagger spesifikasyonu |
| **Swagger UI** | `http://localhost:5089/swagger` | İnteraktif API test paneli |
| **Sistem Sağlık** | `http://localhost:5089/health-ui` | HealthChecks dashboard |
| **Arka Plan İşleri** | `http://localhost:5089/hangfire` | Hangfire job monitoring |
| **RabbitMQ Yönetimi** | `http://localhost:15672` | RabbitMQ Management Console (guest/guest) |
| **Kibana** | `http://localhost:5601` | Elasticsearch veri görselleştirme |
| **Redis Insight** | `http://localhost:8001` | Redis'i izlemek için (isteğe bağlı) |

> **Not:** Port numarası 5089 olarak yapılandırılmıştır. Değişmesi durumunda `launchSettings.json` dosyasını kontrol edin.

---

## 🧪 Simülasyon (Load Testing)

Sisteme yapay yük bindirmek, veri akışını ve kuyruk mekanizmasını gözlemlemek için hazır bash scriptini kullanabilirsiniz:

```bash
chmod +x seed_data.sh
./seed_data.sh
```

**Senaryo:** Bu script, rastgele kripto fiyatlarını API'ye pompalar.

Gözlemlediğiniz Çıktılar:

- **Worker terminalinde:** Logların şelale gibi aktığını (`[RabbitMQ] Price Updated`)
- **API terminalinde:** Gelen istekleri (`[API] POST /api/prices`)
- **Redis durumuyla:** Cache güncellemelerini (`[Redis] Cache Invalidated`)
- **Elasticsearch'te:** İndekslemenin yapıldığını (`[Elastic] Indexed crypto_prices`)

---

## 📂 Proje Yapısı

```
CoinPulse.sln
├── src/
│   ├── CoinPulse.Api/              # Giriş kapısı (Controllers, HealthCheck, Hangfire)
│   │   ├── Controllers/
│   │   │   └── PricesController.cs
│   │   ├── Jobs/
│   │   │   └── MarketReportingJob.cs
│   │   ├── Program.cs
│   │   └── appsettings*.json
│   │
│   ├── CoinPulse.Core/             # Domain Entities, Interfaces, Events (Clean Architecture)
│   │   ├── Entities/
│   │   │   └── CryptoPrice.cs
│   │   ├── Events/
│   │   │   └── PriceUpdatedEvent.cs
│   │   └── Interfaces/
│   │       ├── ICacheService.cs
│   │       └── ISearchService.cs
│   │
│   ├── CoinPulse.Infrastructure/   # DB Context, Redis, Elastic, MassTransit Implementasyonları
│   │   ├── Data/
│   │   │   └── AppDbContext.cs
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs
│   │   ├── Search/
│   │   │   └── ElasticSearchService.cs
│   │   ├── Logging/
│   │   │   └── LoggerSetup.cs
│   │   ├── Migrations/
│   │   └── ServiceRegistration.cs
│   │
│   └── CoinPulse.Worker/           # Arka plan tüketicisi (Consumer)
│       ├── Consumers/
│       │   └── PriceUpdatedConsumer.cs
│       ├── Program.cs
│       ├── Worker.cs
│       └── appsettings*.json
│
├── docker-compose.yml              # Altyapı Konteynerleri (RabbitMQ, Redis, Elastic, Kibana)
├── seed_data.sh                    # Simülasyon Script
└── README.md
```

---

## 🔧 Teknik Detaylar

### Clean Architecture Katmanları

1. **Api Katmanı:** HTTP isteklerini işler, validasyonları yapar ve MassTransit üzerinden mesaj yayınlar.
2. **Core Katmanı:** İş mantığı ve domain modelleri içerir (Entities, Events, Interfaces).
3. **Infrastructure Katmanı:** Dış servislerle iletişim (DB, Redis, Elasticsearch, RabbitMQ).
4. **Worker Katmanı:** Asenkron mesajları tüketir ve arka plan işlemlerini gerçekleştirir.

### Event-Driven Mimari

```
API -> [PriceUpdatedEvent] -> RabbitMQ
                             ↓
                        Worker (Consumer)
                             ↓
                     [Persist to DB]
                             ↓
                     [Update Redis Cache]
                             ↓
                     [Index to Elasticsearch]
```

### Veri Tutarlılığı

- **Redis Distributed Lock:** Eş zamanlı güncellemeleri kontrol eder.
- **Event Sourcing hazırlığı:** Tüm fiyat değişiklikleri olay olarak loglanır.

---

## 📊 Örnek API İstekleri

### Yeni Fiyat Ekle

```bash
curl -X POST http://localhost:5089/api/prices \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTC",
    "price": 97500.00,
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

### Son Fiyatı Getir

```bash
curl http://localhost:5089/api/prices/BTC
```

### Geçmiş Raporlama

```bash
curl http://localhost:5089/api/prices/report?coin=ETH&days=7
```

---

## 🚀 İleri Adımlar

### Üretime Hazırlık
- [ ] API rate limiting ekle
- [ ] JWT authentication implementasyonu
- [ ] Kubernetes deployment manifesti oluştur
- [ ] CI/CD pipeline (GitHub Actions) konfigüre et

### Ölçeklenebilirlik
- [ ] Mikroservislere migration (BFF Pattern)
- [ ] CQRS pattern uygulaması
- [ ] Saga Pattern ile distributed transactions
- [ ] gRPC servisleri

### Monitoring & Observability
- [ ] Prometheus metrikleri
- [ ] Jaeger distributed tracing
- [ ] Custom alerting rules
- [ ] APM (Application Performance Monitoring)

---

## 📝 Lisans

Bu proje kişisel portföl ve eğitim amaçlı hazırlanmıştır.

---

## 👨‍💻 Geliştirici

**İlkay Gökbudak**

Bu proje, modern .NET ekosistemindeki yetkinlikleri ve dağıtık sistem tasarım prensiplerini sergilemek amacıyla hazırlanmıştır.

---

## 📬 Bağlantı

Sorular, öneriler ve geri bildirimler için iletişime geçebilirsiniz.

- **GitHub:** [ilkaygokbudak](https://github.com/ilkaygokbudak)
- **Email:** [ilkay@example.com](mailto:ilkay@example.com)
- **LinkedIn:** [ilkaygokbudak](https://linkedin.com/in/ilkaygokbudak)

---

**Son Güncelleme:** Ocak 2, 2026
