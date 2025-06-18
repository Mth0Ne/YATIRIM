using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Core.Interfaces;
using System.Text.Json;

namespace SmartBIST.Application.Services;

// API hata durumları için özel bir exception
public class PredictionApiException : Exception
{
    public string ErrorCode { get; }
    
    public PredictionApiException(string message, string errorCode = "API_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class PredictionService : IPredictionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PredictionService> _logger;
    private readonly IStockService _stockService;
    private readonly IPredictionApiService _predictionApiService;
    
    public PredictionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStockService stockService,
        IPredictionApiService predictionApiService,
        ILogger<PredictionService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _stockService = stockService;
        _predictionApiService = predictionApiService;
    }
    
    public async Task<PredictionResultDto> GetPricePredictionAsync(PredictionRequestDto requestDto, string userId)
    {
        try
        {
            // Get the stock to fetch the symbol
            var stock = await _unitOfWork.Stocks.GetByIdAsync(requestDto.StockId);
            
            if (stock == null)
            {
                throw new ArgumentException($"Stock with ID {requestDto.StockId} not found");
            }

            // Call the prediction API
            var predictionResponse = await _predictionApiService.GetStockPredictionAsync(
                stock.Symbol,
                requestDto.StartDate,
                requestDto.EndDate
            );
            
            _logger.LogInformation($"API yanıtı: {JsonSerializer.Serialize(predictionResponse)}");
            
            // API yanıtını detaylı incele
            _logger.LogInformation($"API yanıtı: PredictedPrice={predictionResponse.PredictedPrice}, CurrentPrice={predictionResponse.CurrentPrice}, " +
                                   $"PriceChange={predictionResponse.PriceChange}, PercentChange={predictionResponse.PercentChange}, " +
                                   $"DataPoints={predictionResponse.DataPoints}");
            
            // API yanıtını doğrudan kullan - hiçbir hesaplama yapmadan
            if (!predictionResponse.Success || predictionResponse.PredictedPrice <= 0)
            {
                _logger.LogError("Geçersiz API yanıtı. Success={0}, PredictedPrice={1}", 
                    predictionResponse.Success, predictionResponse.PredictedPrice);
                throw new PredictionApiException(
                    predictionResponse.ErrorMessage ?? $"Geçersiz tahmin değeri: {predictionResponse.PredictedPrice}", 
                    "INVALID_API_RESPONSE");
            }
            
            // API yanıtını JSON formatında sakla
            var predictionDataJson = new Dictionary<string, object>
            {
                // API'den gelen tüm alanları ekle - sıfırdan oluşturma, direkt API yanıtını kaydet
                ["symbol"] = predictionResponse.Symbol,
                ["predicted_price"] = predictionResponse.PredictedPrice,
                ["current_price"] = predictionResponse.CurrentPrice,
                ["price_change"] = predictionResponse.PriceChange,
                ["percent_change"] = predictionResponse.PercentChange,
                ["prediction_date"] = predictionResponse.PredictionDate,
                ["last_close_date"] = predictionResponse.LastCloseDate,
                ["data_points"] = predictionResponse.DataPoints,
                // Performance metrics
                ["accuracy"] = predictionResponse.Accuracy,
                ["mae"] = predictionResponse.Mae,
                ["rmse"] = predictionResponse.Rmse,
                ["r2"] = predictionResponse.R2
            };
            
            // Loglama
            _logger.LogInformation($"PredictionDataJson oluşturuldu: {JsonSerializer.Serialize(predictionDataJson)}");
            
            // Create a new prediction record - tüm alanları API'den gelen verilerle doldur
            var prediction = new SmartBIST.Core.Entities.AIStockPrediction
            {
                StockId = requestDto.StockId,
                UserId = userId,
                ModelType = requestDto.ModelType,
                CreatedDate = DateTime.UtcNow,
                PredictionStartDate = requestDto.StartDate,
                PredictionEndDate = requestDto.EndDate,
                
                // API'den gelen tüm değerleri direkt olarak kaydet
                PredictedPrice = (decimal)predictionResponse.PredictedPrice,
                CurrentPrice = (decimal)predictionResponse.CurrentPrice,
                PriceChange = (decimal)predictionResponse.PriceChange,
                PercentChange = (decimal)predictionResponse.PercentChange,
                PredictionDate = predictionResponse.PredictionDate,
                LastCloseDate = predictionResponse.LastCloseDate,
                DataPoints = predictionResponse.DataPoints,
                
                // Parametreler ve ham veriyi de JSON olarak kaydet
                Parameters = JsonSerializer.Serialize(requestDto.Parameters),
                PredictionData = JsonSerializer.Serialize(predictionDataJson, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                
                // API'den gelen gerçek doğruluk değeri
                Accuracy = (decimal)predictionResponse.Accuracy
            };
            
            // Save to database
            await _unitOfWork.AIStockPredictions.AddAsync(prediction);
            await _unitOfWork.SaveChangesAsync();
            
            // Map to DTO and return - API'den gelen tüm değerler ile
            var result = _mapper.Map<PredictionResultDto>(prediction);
            
            // Add stock information
            result.StockSymbol = stock.Symbol;
            result.StockName = stock.Name;
            
            // Tahmin verilerini sonuç DTO'suna doğrudan ekleyelim
            result.PredictionData = predictionDataJson;
            
            // Success bilgisini de ekle
            result.Success = true;
            
            // Son kontrol için loglama
            _logger.LogInformation($"Sonuç DTO oluşturuldu - PredictedPrice: {result.PredictedPrice}, API'den gelen fiyat: {predictionResponse.PredictedPrice}");
            
            return result;
        }
        catch (PredictionApiException ex)
        {
            _logger.LogError(ex, "Prediction API hatası: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price prediction for stock {StockId}", requestDto.StockId);
            throw;
        }
    }
    
    public async Task<IEnumerable<PredictionResultDto>> GetUserPredictionsAsync(string userId)
    {
        try
        {
            var predictions = await _unitOfWork.AIStockPredictions.GetPredictionsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<PredictionResultDto>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for user {UserId}", userId);
            return Enumerable.Empty<PredictionResultDto>();
        }
    }
    
    public async Task<IEnumerable<PredictionResultDto>> GetStockPredictionsAsync(int stockId, string userId)
    {
        try
        {
            var predictions = await _unitOfWork.AIStockPredictions.GetPredictionsByStockIdAsync(stockId);
            var filtered = predictions.Where(p => p.UserId == userId);
            return _mapper.Map<IEnumerable<PredictionResultDto>>(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for stock {StockId}", stockId);
            return Enumerable.Empty<PredictionResultDto>();
        }
    }
    
    public async Task<PredictionResultDto?> GetPredictionByIdAsync(int id, string userId)
    {
        try
        {
            var prediction = await _unitOfWork.AIStockPredictions.GetByIdAsync(id);
            
            if (prediction == null || prediction.UserId != userId)
            {
                return null;
            }
            
            // Get the associated stock
            var stock = await _unitOfWork.Stocks.GetByIdAsync(prediction.StockId);
            var result = _mapper.Map<PredictionResultDto>(prediction);
            
            if (stock != null)
            {
                result.StockSymbol = stock.Symbol;
                result.StockName = stock.Name;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction {Id}", id);
            return null;
        }
    }
    
    public async Task<Dictionary<string, object>> GetMarketInsightsAsync()
    {
        try
        {
            _logger.LogInformation("GetMarketInsightsAsync başlatıldı");
            
            // Direkt veritabanından tüm hisseleri al
            var stocks = await _stockService.GetAllStocksAsync();
            
            if (stocks == null || !stocks.Any())
            {
                _logger.LogWarning("StockService'den hiç hisse gelmedi - Fallback kullanılacak");
                return GetFallbackMarketInsights();
            }

            _logger.LogInformation($"StockService'den {stocks.Count()} hisse alındı");
            
            // DEBUG: İlk birkaç hissenin detaylarını logla
            var firstFewStocks = stocks.Take(3).ToList();
            foreach (var stock in firstFewStocks)
            {
                _logger.LogInformation($"DEBUG Stock: {stock.Symbol} - DailyChangePercentage: {stock.DailyChangePercentage}");
            }
            
            // SADECE DailyChangePercentage değerine göre hesaplama
            var totalStocks = stocks.Count();
            var risingStocks = stocks.Count(s => s.DailyChangePercentage > 0);
            var fallingStocks = stocks.Count(s => s.DailyChangePercentage < 0);
            var unchangedStocks = stocks.Count(s => s.DailyChangePercentage == 0);

            _logger.LogInformation($"HESAPLAMA: Toplam={totalStocks}, Yükselen={risingStocks}, Düşen={fallingStocks}, Değişmeyen={unchangedStocks}");
            
            // Market stats dictionary oluştur
            var marketStats = new Dictionary<string, object>
            {
                ["total_stocks"] = totalStocks,
                ["rising_stocks"] = risingStocks,
                ["falling_stocks"] = fallingStocks,
                ["unchanged_stocks"] = unchangedStocks,
                ["market_breadth"] = totalStocks > 0 ? (risingStocks * 100.0 / totalStocks) : 0
            };
            
            _logger.LogInformation($"Market Stats Dictionary oluşturuldu");
            
            // Piyasa trend hesaplaması
            var marketTrend = risingStocks > fallingStocks ? "Yükseliş" : fallingStocks > risingStocks ? "Düşüş" : "Karışık";
            
            // BIST100 değişimi hesaplaması - tüm hisselerin günlük değişim yüzdelerinin ortalaması
            var averageChangePercent = stocks.Any() ? stocks.Average(s => (double)s.DailyChangePercentage) : 0;
            var bist100ChangeStr = averageChangePercent >= 0 ? $"+{averageChangePercent:F2}" : $"{averageChangePercent:F2}";
            
            _logger.LogInformation($"Piyasa trend hesaplaması: Ortalama değişim yüzdesi={averageChangePercent:F2}%");

            // Ana sonuç dictionary'si
            var result = new Dictionary<string, object>
            {
                ["market_trend"] = marketTrend,
                ["bist100_change"] = bist100ChangeStr,
                ["market_summary"] = $"Veritabanından: {totalStocks} hisse, {risingStocks} yükselen, {fallingStocks} düşen (Ort. değişim: {averageChangePercent:F2}%)",
                ["market_stats"] = marketStats  // ÖNEMLİ: Bu anahtar mutlaka olmalı
            };
            
            _logger.LogInformation($"Sonuç döndürülüyor - market_stats anahtarı eklendi");
            
            // DEBUG: Döndürülen key'leri logla
            var resultKeys = string.Join(", ", result.Keys);
            _logger.LogInformation($"Döndürülen anahtarlar: {resultKeys}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMarketInsightsAsync'de hata oluştu");
            return GetFallbackMarketInsights();
        }
    }
    
    private Dictionary<string, object> GetFallbackMarketInsights()
    {
        _logger.LogWarning("GetFallbackMarketInsights çağrıldı - gerçek veri bulunamadı");
        
        // Fallback data with realistic market_stats
        var marketStats = new Dictionary<string, object>
        {
            ["total_stocks"] = 0,
            ["rising_stocks"] = 0,
            ["falling_stocks"] = 0,
            ["unchanged_stocks"] = 0,
            ["market_breadth"] = 0
        };

        return new Dictionary<string, object>
        {
            ["market_trend"] = "Veri Yok",
            ["bist100_change"] = "0.00",
            ["market_summary"] = "Veritabanından veri alınamadı - lütfen hisse verilerini kontrol edin",
            ["market_stats"] = marketStats  // ÖNEMLİ: Bu anahtar mutlaka olmalı
        };
    }
} 