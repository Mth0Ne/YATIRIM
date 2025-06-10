using System.Text.Json;
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
                
                _logger.LogInformation($"Ayrıştırılmış API yanıtı: Symbol={apiResponse?.Symbol}, PredictedPrice={apiResponse?.PredictedPrice}, CurrentPrice={apiResponse?.CurrentPrice}, PriceChange={apiResponse?.PriceChange}, PercentChange={apiResponse?.PercentChange}");
                
                // Eğer apiResponse birçok alanı null veya 0 içeriyorsa, manuel olarak JSON'dan değerleri çıkarmayı deneyelim
                if (apiResponse?.PredictedPrice == 0 || apiResponse?.CurrentPrice == 0)
                {
                    _logger.LogWarning("API yanıtında bazı alanlar eksik, manuel ayrıştırma deneniyor");
                    
                    try 
                    {
                        // Manuel JSON ayrıştırma deneyelim
                        using JsonDocument doc = JsonDocument.Parse(content);
                        JsonElement root = doc.RootElement;
                        
                        // Değerlere bakalım - API'deki alan isimlerini kullan
                        if (root.TryGetProperty("predicted_price", out JsonElement predictedPrice) && apiResponse != null)
                        {
                            apiResponse.PredictedPrice = predictedPrice.GetDouble();
                            _logger.LogInformation($"Manuel ayrıştırılan PredictedPrice: {apiResponse.PredictedPrice}");
                        }
                        
                        if (root.TryGetProperty("current_price", out JsonElement currentPrice) && apiResponse != null)
                        {
                            apiResponse.CurrentPrice = currentPrice.GetDouble();
                            _logger.LogInformation($"Manuel ayrıştırılan CurrentPrice: {apiResponse.CurrentPrice}");
                        }
                        
                        if (root.TryGetProperty("price_change", out JsonElement priceChange) && apiResponse != null)
                        {
                            apiResponse.PriceChange = priceChange.GetDouble();
                            _logger.LogInformation($"Manuel ayrıştırılan PriceChange: {apiResponse.PriceChange}");
                        }
                        
                        if (root.TryGetProperty("percent_change", out JsonElement percentChange) && apiResponse != null)
                        {
                            apiResponse.PercentChange = percentChange.GetDouble();
                            _logger.LogInformation($"Manuel ayrıştırılan PercentChange: {apiResponse.PercentChange}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Manuel JSON ayrıştırma hatası");
                    }
                }
                
                // Tahmin edilen fiyat hala sıfır ise ve API başarılı dönüş yaptıysa, bu durumu loglayalım
                if (apiResponse?.PredictedPrice == 0)
                {
                    _logger.LogWarning($"API başarılı yanıt döndü ancak tahmin edilen fiyat 0: {content}");
                }
                
                // Map to the Application interface response type
                return new PredictionApiResponse 
                { 
                    Symbol = apiResponse?.Symbol ?? symbol, 
                    PredictedPrice = apiResponse?.PredictedPrice ?? 0,
                    CurrentPrice = apiResponse?.CurrentPrice ?? 0,
                    PriceChange = apiResponse?.PriceChange ?? 0,
                    PercentChange = apiResponse?.PercentChange ?? 0,
                    PredictionDate = apiResponse?.PredictionDate ?? string.Empty,
                    LastCloseDate = apiResponse?.LastCloseDate ?? string.Empty,
                    DataPoints = apiResponse?.DataPoints ?? 0,
                    // Performance metrics
                    Accuracy = apiResponse?.Accuracy ?? 0,
                    Mae = apiResponse?.Mae ?? 0,
                    Rmse = apiResponse?.Rmse ?? 0,
                    R2 = apiResponse?.R2 ?? 0,
                    Success = true,
                    ErrorMessage = null
                };
            }
            else
            {
                // Hata durumları için JSON yanıtını okuyalım
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Received error response: Status {(int)response.StatusCode} - {errorContent}");
                
                // 400 Bad Request durumunda genellikle API bir hata açıklaması gönderir
                ErrorResponse? errorResponse = null;
                try
                {
                    errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                    // JSON ayrıştırma hatası - varsayılan hata mesajı kullan
                }

                string errorMessage;
                if (errorResponse?.Error != null)
                {
                    errorMessage = errorResponse.Error;
                }
                else
                {
                    errorMessage = $"API Hatası: {(int)response.StatusCode} - {response.ReasonPhrase}";
                }

                // Hatalı durum için bir cevap döndür ama exception fırlatma
                return new PredictionApiResponse
                {
                    Symbol = symbol,
                    PredictedPrice = 0,
                    CurrentPrice = 0,
                    PriceChange = 0,
                    PercentChange = 0,
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling stock prediction API for symbol {symbol}");
            return new PredictionApiResponse
            {
                Symbol = symbol,
                PredictedPrice = 0,
                CurrentPrice = 0,
                PriceChange = 0,
                PercentChange = 0,
                Success = false,
                ErrorMessage = $"API ile bağlantı hatası: {ex.Message}"
            };
        }
    }
    
    // Private class to deserialize the API response
    private class ApiResponse
    {
        public string? Symbol { get; set; }
        public double PredictedPrice { get; set; }
        public double CurrentPrice { get; set; }
        public double PriceChange { get; set; }
        public double PercentChange { get; set; }
        public string? PredictionDate { get; set; }
        public string? LastCloseDate { get; set; }
        public int DataPoints { get; set; }
        // Performance metrics
        public double Accuracy { get; set; }
        public double Mae { get; set; }
        public double Rmse { get; set; }
        public double R2 { get; set; }
    }
    
    // Hata yanıtlarını ayrıştırmak için
    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
} 