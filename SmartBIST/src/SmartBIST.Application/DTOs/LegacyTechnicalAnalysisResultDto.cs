namespace SmartBIST.Application.DTOs;

// Legacy DTO for backward compatibility with existing TechnicalIndicatorService
public class LegacyTechnicalAnalysisResultDto
{
    public int StockId { get; set; }
    public string StockSymbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public DateTime AnalysisDate { get; set; }
    public Dictionary<string, object> Indicators { get; set; } = new();
    public Dictionary<string, object> Signals { get; set; } = new();
    public string OverallSignal { get; set; } = string.Empty;
    public double SignalStrength { get; set; }
} 