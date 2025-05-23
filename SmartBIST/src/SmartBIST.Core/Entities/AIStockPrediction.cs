namespace SmartBIST.Core.Entities;

public enum PredictionModel
{
    LSTM,
    RandomForest,
    XGBoost,
    LinearRegression,
    Prophet
}

public class AIStockPrediction
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public PredictionModel ModelType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime PredictionStartDate { get; set; }
    public DateTime PredictionEndDate { get; set; }
    
    // API'den gelen tahmin verileri
    public decimal PredictedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PercentChange { get; set; }
    public string PredictionDate { get; set; } = string.Empty;
    public string LastCloseDate { get; set; } = string.Empty;
    public int DataPoints { get; set; }
    
    // İlk oluşturmadan gelen parametreler ve tüm verilerin JSON temsili 
    public string Parameters { get; set; } = string.Empty; // JSON string with parameters
    public string PredictionData { get; set; } = string.Empty; // JSON string with all prediction data
    
    // Modelin performans metriği
    public decimal Accuracy { get; set; } // 0-1 arası doğruluk değeri
    
    // Navigation properties
    public virtual Stock Stock { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
} 