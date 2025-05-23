namespace SmartBIST.Core.Entities;

public enum IndicatorType
{
    RSI,
    MACD,
    BollingerBands,
    MovingAverage,
    StochasticOscillator
}

public class TechnicalIndicator
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public IndicatorType Type { get; set; }
    public DateTime Date { get; set; }
    public string Parameters { get; set; } = string.Empty; // JSON string with parameters
    public string Value { get; set; } = string.Empty; // JSON string with calculated values
    
    // Navigation property
    public virtual Stock Stock { get; set; } = null!;
} 