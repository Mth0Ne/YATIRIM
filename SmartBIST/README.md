# SmartBIST - Akıllı Borsa İstanbul Portföy Yönetim Sistemi

SmartBIST, Borsa İstanbul'da işlem gören hisse senetleri için yapay zeka destekli portföy yönetimi sağlayan kapsamlı bir web uygulamasıdır.

## Özellikler

- **Kullanıcı Yönetimi**: ASP.NET Core Identity ile güvenli kimlik doğrulama
- **Portföy Yönetimi**: Hisse senedi alım-satım işlemleri, portföy oluşturma ve izleme
- **Veri Entegrasyonu**: BIST hisse verileri için Python Flask API entegrasyonu
- **Yapay Zeka**: Hisse senedi fiyat tahmini ve portföy optimizasyonu
- **Teknik Analiz**: RSI, MACD, Bollinger Bands gibi teknik göstergeler
- **Performans Takibi**: Kar/zarar ve portföy performansı analizi
- **Grafiksel Dashboard**: Portföy performansını görselleştirme

## Teknoloji Yığını

- **.NET Core 8.0**: Modern ve yüksek performanslı web uygulaması geliştirme
- **Çok Katmanlı Mimari**: Core, Application, Infrastructure ve WebUI katmanları
- **Entity Framework Core**: SQL Server veritabanı bağlantısı
- **ASP.NET Core MVC**: Kullanıcı arayüzü
- **Flask API**: Python ile hisse senedi verisi ve yapay zeka tahminleri
- **Bootstrap**: Responsive ve modern UI tasarımı
- **Chart.js**: Veri görselleştirme

## Proje Yapısı

- **SmartBIST.Core**: Entity'ler, arayüzler ve temel modeller
- **SmartBIST.Application**: İş kuralları, servisler ve DTO'lar
- **SmartBIST.Infrastructure**: Veritabanı, veri erişim ve harici servis entegrasyonu
- **SmartBIST.WebUI**: ASP.NET Core MVC kullanıcı arayüzü
- **FlaskAPI**: Python ile yazılmış hisse senedi verisi ve tahmin API'si

## Başlangıç

### Gereksinimler

- .NET Core 8.0 SDK
- SQL Server (LocalDB veya SQL Server örneği)
- Python 3.8+ (Flask API için)
- Node.js (npm paketleri için)

### Kurulum

1. Projeyi klonlayın:
   ```
   git clone https://github.com/yourusername/SmartBIST.git
   cd SmartBIST
   ```

2. Veritabanını oluşturun:
   ```
   cd src
   dotnet ef database update --project SmartBIST.Infrastructure --startup-project SmartBIST.WebUI
   ```

3. Web uygulamasını çalıştırın:
   ```
   cd SmartBIST.WebUI
   dotnet run
   ```

4. Flask API'yi kurun ve çalıştırın:
   ```
   cd ../FlaskAPI
   python -m venv venv
   venv\Scripts\activate  # Windows için
   pip install -r requirements.txt
   python app.py
   ```

5. Web tarayıcınızda uygulamayı açın:
   ```
   https://localhost:5001
   ```

## Mimariye Genel Bakış

### Core Layer

Temel varlık modellerini, arayüzleri ve domain modellerini içerir.

### Application Layer

İş mantığını, servis arayüzlerini, DTO'ları ve validasyon kurallarını içerir.

### Infrastructure Layer

Veritabanı yapılandırması, repository implementasyonları ve harici API entegrasyonlarını içerir.

### WebUI Layer

Kullanıcı arayüzü, controllers, views ve view model'ları içerir.

### Flask API

Hisse senedi verileri ve yapay zeka tahminleri sağlayan Python tabanlı API.

## Katkıda Bulunma

1. Bu repository'yi fork edin
2. Feature branch'ınızı oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some amazing feature'`)
4. Branch'ınıza push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## Lisans

Bu proje MIT Lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın. 