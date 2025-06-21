using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.Services;

namespace SmartBIST.Infrastructure.Services;

public class PredictionApiService : IPredictionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PredictionApiService> _logger;
    private readonly string _baseUrl;

    public PredictionApiService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<PredictionApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ApiSettings:StockPredictionApiUrl"] ?? "http://localhost:5000";
        
        // Prediction işlemleri uzun sürebilir, timeout'u artır
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 10 dakika timeout
    }

    public async Task<PredictionApiResponse> GetStockPredictionAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Borsa İstanbul hisse senetleri için .IS ekle
            if (!symbol.Contains("."))
            {
                symbol = $"{symbol}.IS";
            }
            
            _logger.LogInformation($"Prediction API called with symbol: {symbol}");
            
            var requestUrl = $"{_baseUrl}/predict?symbol={Uri.EscapeDataString(symbol)}&start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}";
            _logger.LogInformation($"Sending prediction request to: {requestUrl}");
            
            var response = await _httpClient.GetAsync(requestUrl);
            
            // Başarılı yanıt durumunda
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received successful response: {content}");
                
                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation($"Ayrıştırılmış API yanıtı: Symbol={apiResponse?.Symbol}, PredictedPrice={apiResponse?.PredictedPrice}, CurrentPrice={apiResponse?.CurrentPrice}, PriceChange={apiResponse?.PriceChange}, PercentChange={apiResponse?.PercentChange}, DataPoints={apiResponse?.DataPoints}");
                
                return new PredictionApiResponse
                {
                    Symbol = apiResponse?.Symbol ?? symbol,
                    PredictedPrice = apiResponse?.PredictedPrice ?? 0,
                    CurrentPrice = apiResponse?.CurrentPrice ?? 0,
                    PriceChange = apiResponse?.PriceChange ?? 0,
                    PercentChange = apiResponse?.PercentChange ?? 0,
                    PredictionDate = apiResponse?.PredictionDate ?? DateTime.Now.ToString("yyyy-MM-dd"),
                    LastCloseDate = apiResponse?.LastCloseDate ?? DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                    DataPoints = apiResponse?.DataPoints ?? 0,
                    Accuracy = apiResponse?.Accuracy ?? 0,
                    Mae = apiResponse?.Mae ?? 0,
                    Rmse = apiResponse?.Rmse ?? 0,
                    R2 = apiResponse?.R2 ?? 0,
                    Success = apiResponse?.PredictedPrice > 0,
                    ErrorMessage = apiResponse?.PredictedPrice > 0 ? null : "API'den geçersiz tahmin değeri alındı"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"API request failed with status {response.StatusCode}: {errorContent}");
                
                return new PredictionApiResponse
                {
                    Symbol = symbol,
                    Success = false,
                    ErrorMessage = $"API Hatası ({response.StatusCode}): {errorContent}"
                };
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Prediction API request timed out for symbol {Symbol}", symbol);
            return new PredictionApiResponse
            {
                Symbol = symbol,
                Success = false,
                ErrorMessage = "İstek zaman aşımına uğradı. Model eğitimi çok uzun sürdü. Lütfen daha sonra tekrar deneyin."
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Prediction API request was cancelled for symbol {Symbol}", symbol);
            return new PredictionApiResponse
            {
                Symbol = symbol,
                Success = false,
                ErrorMessage = "İstek iptal edildi veya zaman aşımına uğradı."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during prediction request for symbol {Symbol}", symbol);
            return new PredictionApiResponse
            {
                Symbol = symbol,
                Success = false,
                ErrorMessage = $"API bağlantı hatası: {ex.Message}. Python API'nin çalıştığından emin olun."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during prediction request for symbol {Symbol}", symbol);
            return new PredictionApiResponse
            {
                Symbol = symbol,
                Success = false,
                ErrorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}"
            };
        }
    }
    
    // Private class to deserialize the API response
    private class ApiResponse
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }
        
        [JsonPropertyName("predicted_price")]
        public double PredictedPrice { get; set; }
        
        [JsonPropertyName("current_price")]
        public double CurrentPrice { get; set; }
        
        [JsonPropertyName("price_change")]
        public double PriceChange { get; set; }
        
        [JsonPropertyName("percent_change")]
        public double PercentChange { get; set; }
        
        [JsonPropertyName("prediction_date")]
        public string? PredictionDate { get; set; }
        
        [JsonPropertyName("last_close_date")]
        public string? LastCloseDate { get; set; }
        
        [JsonPropertyName("data_points")]
        public int DataPoints { get; set; }
        
        // Performance metrics - Python API'den düz field'lar olarak geliyor artık
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }
        
        [JsonPropertyName("mae")]
        public double Mae { get; set; }
        
        [JsonPropertyName("rmse")]
        public double Rmse { get; set; }
        
        [JsonPropertyName("r2")]
        public double R2 { get; set; }
    }
    
    // Hata yanıtlarını ayrıştırmak için
    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
} 