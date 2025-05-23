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
                ["data_points"] = predictionResponse.DataPoints
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
                
                // Sabit doğruluk değeri (gerçek bir LSTM modelinde dinamik hesaplanabilir)
                Accuracy = 0.8m
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
            // Get all stocks to calculate real market insights
            var stocks = await _stockService.GetAllStocksAsync();
            
            if (stocks == null || !stocks.Any())
            {
                return GetFallbackMarketInsights();
            }
            
            // Calculate overall market trend based on average change percentage
            var avgChange = stocks.Average(s => s.DailyChangePercentage);
            var trendDescription = avgChange > 1 ? "Güçlü yükseliş" : 
                                  avgChange > 0 ? "Yükseliş" : 
                                  avgChange > -1 ? "Yatay" : "Düşüş";
            
            // Find top performing sector
            var sectorPerformance = stocks
                .Where(s => !string.IsNullOrEmpty(s.Sector))
                .GroupBy(s => s.Sector)
                .Select(g => new { 
                    Sector = g.Key, 
                    AvgChange = g.Average(s => s.DailyChangePercentage)
                })
                .OrderByDescending(s => s.AvgChange)
                .FirstOrDefault();
                
            var topSector = sectorPerformance?.Sector ?? "Belirsiz";
            var sectorChange = sectorPerformance?.AvgChange.ToString("0.00") ?? "0.00";
            
            // Find market movers (top gainers and losers)
            var marketMovers = stocks
                .OrderByDescending(s => s.DailyChangePercentage)
                .Take(5)
                .Concat(stocks.OrderBy(s => s.DailyChangePercentage).Take(2))
                .Select(s => new Dictionary<string, string> {
                    ["symbol"] = s.Symbol,
                    ["name"] = s.Name,
                    ["change_percentage"] = s.DailyChangePercentage.ToString("0.00")
                })
                .ToList();
            
            // Generate simple predicted trends based on recent performance
            // In a real app, this would use more sophisticated analysis or ML models
            var predictedTrends = stocks
                .GroupBy(s => s.Sector)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Take(3)
                .Select(g => {
                    var avgSectorChange = g.Average(s => s.DailyChangePercentage);
                    string prediction = avgSectorChange > 1 ? "Yükseliş" : 
                                       avgSectorChange > 0 ? "Yatay/Yükseliş" : 
                                       avgSectorChange > -1 ? "Yatay" : "Düşüş";
                    
                    // Confidence is just a simplified metric based on consistency
                    var stdDev = Math.Sqrt(g.Average(s => Math.Pow((double)s.DailyChangePercentage - (double)avgSectorChange, 2)));
                    var confidence = 100 - Math.Min(95, stdDev * 10);
                    
                    return new { 
                        sector = g.Key, 
                        prediction = prediction, 
                        confidence = $"{confidence:0}%" 
                    };
                })
                .ToList();
            
            // Generate market summary
            var topGainer = stocks.OrderByDescending(s => s.DailyChangePercentage).FirstOrDefault();
            var topLoser = stocks.OrderBy(s => s.DailyChangePercentage).FirstOrDefault();
            
            string marketSummary = $"Borsa İstanbul bugün {trendDescription.ToLower()} eğilimi gösterdi, BIST 100 endeksi %{avgChange:0.00} değişim gösterdi. ";
            
            if (topGainer != null && topLoser != null)
            {
                marketSummary += $"En yüksek performans gösteren hisse {topGainer.Symbol} (%{topGainer.DailyChangePercentage:0.00}), " +
                                $"en düşük performans gösteren hisse ise {topLoser.Symbol} (%{topLoser.DailyChangePercentage:0.00}) oldu. ";
            }
            
            if (!string.IsNullOrEmpty(topSector))
            {
                marketSummary += $"{topSector} sektörü %{sectorChange} değişimle öne çıktı.";
            }
            
            // Put everything together
            var insights = new Dictionary<string, object>
            {
                ["market_trend"] = trendDescription,
                ["bist100_change"] = avgChange.ToString("0.00"),
                ["top_sector"] = topSector,
                ["sector_change"] = sectorChange,
                ["market_summary"] = marketSummary,
                ["market_movers"] = marketMovers,
                ["predicted_trends"] = predictedTrends
            };
            
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Piyasa bilgileri alınırken hata oluştu");
            return GetFallbackMarketInsights();
        }
    }
    
    private Dictionary<string, object> GetFallbackMarketInsights()
    {
        // Fallback data in case of errors
        return new Dictionary<string, object>
        {
            ["market_trend"] = "Belirsiz",
            ["bist100_change"] = "0.0",
            ["error"] = "Piyasa bilgileri alınamadı",
            ["market_summary"] = "Piyasa verileri şu anda erişilebilir değil.",
            ["market_movers"] = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { ["symbol"] = "THYAO", ["name"] = "Türk Hava Yolları", ["change_percentage"] = "0.0" },
                new Dictionary<string, string> { ["symbol"] = "GARAN", ["name"] = "Garanti Bankası", ["change_percentage"] = "0.0" }
            }
        };
    }
} 