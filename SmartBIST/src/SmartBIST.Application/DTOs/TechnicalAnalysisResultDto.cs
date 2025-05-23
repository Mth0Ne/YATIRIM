namespace SmartBIST.Application.DTOs;

public class TechnicalAnalysisResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public DateTime AnalysisDate { get; set; }
    public int PeriodDays { get; set; }
    public int DataPoints { get; set; }
    public Dictionary<string, object> Indicators { get; set; } = new();
    public TechnicalSignalsDto Signals { get; set; } = new();
    public List<PriceDataDto> PriceHistory { get; set; } = new();
}

public class TechnicalSignalsDto
{
    public Dictionary<string, string> IndividualSignals { get; set; } = new();
    public string OverallSignal { get; set; } = string.Empty;
    public double SignalStrength { get; set; }
    public int BuySignals { get; set; }
    public int SellSignals { get; set; }
    public int NeutralSignals { get; set; }
}

public class PriceDataDto
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class PriceHistoryResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public List<PriceDataDto> PriceHistory { get; set; } = new();
    public int DataPoints { get; set; }
} 