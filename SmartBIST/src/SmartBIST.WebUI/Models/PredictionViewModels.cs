using System.ComponentModel.DataAnnotations;
using SmartBIST.Core.Entities;

namespace SmartBIST.WebUI.Models;

public class CreatePredictionViewModel
{
    [Required]
    [Display(Name = "Stock")]
    public int StockId { get; set; }
    
    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    
    [Required]
    [Display(Name = "Prediction Model")]
    public int ModelType { get; set; }
    
    [Display(Name = "Training Window (days)")]
    [Range(30, 365, ErrorMessage = "Training window must be between 30 and 365 days")]
    public int? TrainingWindow { get; set; } = 60;
    
    [Display(Name = "Confidence Level")]
    [Range(0.8, 0.99, ErrorMessage = "Confidence level must be between 0.8 and 0.99")]
    public double? ConfidenceLevel { get; set; } = 0.95;
    
    [Display(Name = "Include Technical Indicators")]
    public bool IncludeTechnicalIndicators { get; set; } = true;
    
    [Display(Name = "Include Sentiment Analysis")]
    public bool IncludeSentimentAnalysis { get; set; } = false;
    
    public Dictionary<string, string> GetParametersDictionary()
    {
        var parameters = new Dictionary<string, string>();
        
        if (TrainingWindow.HasValue)
        {
            parameters.Add("training_window", TrainingWindow.Value.ToString());
        }
        
        if (ConfidenceLevel.HasValue)
        {
            parameters.Add("confidence_level", ConfidenceLevel.Value.ToString("F2"));
        }
        
        parameters.Add("include_technical_indicators", IncludeTechnicalIndicators.ToString().ToLower());
        parameters.Add("include_sentiment_analysis", IncludeSentimentAnalysis.ToString().ToLower());
        
        return parameters;
    }
}

public class PredictionDetailViewModel
{
    // Temel bilgiler
    public int Id { get; set; }
    public string StockSymbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public PredictionModel ModelType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Model parametreleri
    public int TrainingWindow { get; set; }
    public double ConfidenceLevel { get; set; } = 0.95;
    public bool IncludeTechnicalIndicators { get; set; }
    public bool IncludeSentimentAnalysis { get; set; }
    
    // API'den gelen tahmin değerleri
    public decimal PredictedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PercentChange { get; set; }
    public string PredictionDate { get; set; } = string.Empty;
    public string LastCloseDate { get; set; } = string.Empty;
    public int DataPoints { get; set; }
    
    // API yanıt durumu
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; } = null;
    public bool IsPredictionDataMissing { get; set; } = false;
    
    // Performans metrikleri
    public decimal Accuracy { get; set; }
    public decimal MeanAbsoluteError { get; set; }
    public decimal RootMeanSquaredError { get; set; }
    public decimal RSquared { get; set; }
    
    // Grafik verileri
    public List<decimal> ActualPrices { get; set; } = new();
    public List<decimal> PredictedPrices { get; set; } = new();
    public List<string> DateLabels { get; set; } = new();
}

public class PredictionListItemViewModel
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string StockSymbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public PredictionModel ModelType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public Dictionary<string, string>? Metrics { get; set; }
    public List<decimal> ActualValues { get; set; } = new();
    public List<decimal> PredictedValues { get; set; } = new();
    public decimal PredictedPrice { get; set; }
    public decimal Accuracy { get; set; }
} 