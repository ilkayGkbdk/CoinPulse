# 1. Build Aşaması (ÖNEMLİ: 'AS build' takma adı burada tanımlanıyor)
# .NET 10 Preview sürümü için genellikle 'preview' etiketi gerekir.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Proje dosyalarını kopyala
COPY CoinPulse.sln ./
COPY src/CoinPulse.Api/*.csproj ./src/CoinPulse.Api/
COPY src/CoinPulse.Worker/*.csproj ./src/CoinPulse.Worker/
COPY src/CoinPulse.Core/*.csproj ./src/CoinPulse.Core/
COPY src/CoinPulse.Infrastructure/*.csproj ./src/CoinPulse.Infrastructure/
# Shared referansı kaldırıldı

# Bağımlılıkları yükle
RUN dotnet restore

# Tüm kodları kopyala ve derle
COPY . .
WORKDIR /app/src/CoinPulse.Api
RUN dotnet publish -c Release -o /app/out

# 2. Run Aşaması
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "CoinPulse.Api.dll"]