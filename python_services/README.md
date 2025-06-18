# Python Services

Bu klasör, SmartBIST projesinin Python tabanlı servislerini içerir.

## Klasör Yapısı

```
python_services/
├── api/                    # API endpoint'leri
│   ├── api.py             # Ana API servisi
│   └── technical_analysis_api.py  # Teknik analiz API'si
├── models/                 # ML modelleri
│   └── lstm_model.h5      # LSTM model dosyası
├── utils/                  # Yardımcı fonksiyonlar
├── config/                 # Yapılandırma dosyaları
├── logs/                   # Log dosyaları
│   ├── api.log
│   └── technical_analysis.log
├── requirements.txt        # Python bağımlılıkları
└── README.md              # Bu dosya
```

## Kurulum

1. Python 3.8 veya üstü sürümün yüklü olduğundan emin olun
2. Sanal ortam oluşturun ve aktifleştirin:
   ```bash
   python -m venv venv
   source venv/bin/activate  # Linux/Mac
   # veya
   .\venv\Scripts\activate  # Windows
   ```
3. Bağımlılıkları yükleyin:
   ```bash
   pip install -r requirements.txt
   ```

## Çalıştırma

1. API servisini başlatmak için:
   ```bash
   cd api
   python api.py
   ```

2. Teknik analiz API'sini başlatmak için:
   ```bash
   cd api
   python technical_analysis_api.py
   ```

## API Endpoint'leri

### Ana API (api.py)
- `GET /`: Ana sayfa
- `GET /predict`: Hisse senedi tahmini

### Teknik Analiz API (technical_analysis_api.py)
- `GET /`: Ana sayfa
- `GET /technical-analysis/<symbol>`: Teknik analiz göstergeleri
- `GET /price-history/<symbol>`: Fiyat geçmişi

## Notlar

- Model dosyaları `models/` klasöründe saklanır
- Log dosyaları `logs/` klasöründe tutulur
- Yapılandırma değişkenleri `.env` dosyasında tanımlanır 