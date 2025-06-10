namespace SmartBIST.Application.Services;

/// <summary>
/// Interface for the stock prediction API service
/// </summary>
public interface IPredictionApiService
{
    Task<PredictionApiResponse> GetStockPredictionAsync(string symbol, DateTime startDate, DateTime endDate);
}

/// <summary>
/// Response model from the prediction API that exactly matches the API's JSON structure
/// </summary>
public class PredictionApiResponse
{
    // API'nin döndürdüğü tam alan yapısı - API'de tanımlanan tüm alanlar burada olmalı
    public string Symbol { get; set; } = string.Empty;
    public double PredictedPrice { get; set; }
    public double CurrentPrice { get; set; }
    public double PriceChange { get; set; }
    public double PercentChange { get; set; }
    public string PredictionDate { get; set; } = string.Empty;
    public string LastCloseDate { get; set; } = string.Empty;
    public int DataPoints { get; set; }
    
    // Performance metrics from Python API
    public double Accuracy { get; set; }
    public double Mae { get; set; }
    public double Rmse { get; set; }
    public double R2 { get; set; }
    
    // API yanıt durumunu temsil eden alanlar - API çağrısı yönetimi için
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
} 