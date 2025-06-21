namespace SmartBIST.Application.DTOs;

public class DataMiningAnalysisResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public DateTime AnalysisDate { get; set; }
    public int PeriodDays { get; set; }
    public int DataPoints { get; set; }
    public string AnalysisType { get; set; } = "data_mining";
    
    // Klasik teknik analiz
    public Dictionary<string, object> ClassicIndicators { get; set; } = new();
    
    // Veri madenciliği özellikleri
    public AdvancedFeaturesDto AdvancedFeatures { get; set; } = new();
    public ChartPatternsDto ChartPatterns { get; set; } = new();
    public AnomaliesDto Anomalies { get; set; } = new();
    public ClusteringDto Clustering { get; set; } = new();
    public StatisticalTestsDto StatisticalTests { get; set; } = new();
    public RiskAnalysisDto RiskAnalysis { get; set; } = new();
    public AdvancedTechnicalDto AdvancedTechnical { get; set; } = new();
    public PredictionsDto? Predictions { get; set; }
    
    // Sinyaller
    public TechnicalSignalsDto Signals { get; set; } = new();
    
    // Fiyat geçmişi
    public List<PriceDataDto> PriceHistory { get; set; } = new();
}

public class AdvancedFeaturesDto
{
    public double PriceVolatility { get; set; }
    public double PriceMean { get; set; }
    public double PriceSkewness { get; set; }
    public double PriceKurtosis { get; set; }
    public double VolumeMean { get; set; }
    public double VolumeVolatility { get; set; }
    public double VolumeTrend { get; set; }
    public double PriceVolumeCorrelation { get; set; }
    public double Momentum1D { get; set; }
    public double Momentum5D { get; set; }
    public double Momentum20D { get; set; }
    public double TrendSlope { get; set; }
    public double TrendRSquared { get; set; }
    public double TrendPValue { get; set; }
    public double HLSpreadMean { get; set; }
    public double HLSpreadVolatility { get; set; }
    public double PricePosition { get; set; }
    public double SMA5SMA20Ratio { get; set; }
}

public class ChartPatternsDto
{
    public List<double> ResistanceLevels { get; set; } = new();
    public List<double> SupportLevels { get; set; } = new();
    public double? LastResistance { get; set; }
    public double? LastSupport { get; set; }
    public bool? DoubleTop { get; set; }
    public double? DoubleTopLevel { get; set; }
    public bool? DoubleBottom { get; set; }
    public double? DoubleBottomLevel { get; set; }
    public double? ChannelWidth { get; set; }
    public bool? InChannel { get; set; }
}

public class AnomaliesDto
{
    public int TotalAnomalies { get; set; }
    public double AnomalyRatio { get; set; }
    public bool RecentAnomalies { get; set; }
    public double AnomalyScore { get; set; }
    public int StatisticalAnomalies { get; set; }
    public int VolumeAnomalies { get; set; }
}

public class ClusteringDto
{
    public List<List<double>> ClusterCenters { get; set; } = new();
    public List<int> ClusterLabels { get; set; } = new();
    public double Inertia { get; set; }
    public Dictionary<string, ClusterStatDto> ClusterStatistics { get; set; } = new();
    public int? CurrentCluster { get; set; }
}

public class ClusterStatDto
{
    public int Size { get; set; }
    public double MeanReturn { get; set; }
    public double MeanVolumeChange { get; set; }
    public double Volatility { get; set; }
}

public class StatisticalTestsDto
{
    public ADFTestDto? ADFTest { get; set; }
    public NormalityTestDto? NormalityTest { get; set; }
    public AutocorrelationDto? Autocorrelation { get; set; }
    public PriceVolumeCorrelationDto? PriceVolumeCorrelation { get; set; }
}

public class ADFTestDto
{
    public double Statistic { get; set; }
    public double PValue { get; set; }
    public bool IsStationary { get; set; }
    public Dictionary<string, double> CriticalValues { get; set; } = new();
}

public class NormalityTestDto
{
    public double Statistic { get; set; }
    public double PValue { get; set; }
    public bool IsNormal { get; set; }
}

public class AutocorrelationDto
{
    public double Lag1 { get; set; }
    public double Lag5 { get; set; }
}

public class PriceVolumeCorrelationDto
{
    public double Correlation { get; set; }
    public double PValue { get; set; }
    public bool IsSignificant { get; set; }
}

public class RiskAnalysisDto
{
    public ValueAtRiskDto? ValueAtRisk { get; set; }
    public VolatilityDto? Volatility { get; set; }
    public double? SharpeRatio { get; set; }
    public double? MaxDrawdown { get; set; }
    public double? Beta { get; set; }
}

public class ValueAtRiskDto
{
    public double VAR95 { get; set; }
    public double VAR99 { get; set; }
    public double ExpectedShortfall95 { get; set; }
}

public class VolatilityDto
{
    public double Daily { get; set; }
    public double Annualized { get; set; }
    public double Rolling30D { get; set; }
}

public class AdvancedTechnicalDto
{
    public FibonacciDto? Fibonacci { get; set; }
    public PivotPointsDto? PivotPoints { get; set; }
    public IchimokuDto? Ichimoku { get; set; }
}

public class FibonacciDto
{
    public double High { get; set; }
    public double Low { get; set; }
    public Dictionary<string, double> Levels { get; set; } = new();
}

public class PivotPointsDto
{
    public double Pivot { get; set; }
    public double Resistance1 { get; set; }
    public double Support1 { get; set; }
    public double Resistance2 { get; set; }
    public double Support2 { get; set; }
}

public class IchimokuDto
{
    public double TenkanSen { get; set; }
    public double KijunSen { get; set; }
    public double CloudTop { get; set; }
    public double CloudBottom { get; set; }
    public string PricePosition { get; set; } = string.Empty;
}

public class PredictionsDto
{
    public LinearTrendDto? LinearTrend { get; set; }
    public MovingAverageSignalDto? MovingAverageSignal { get; set; }
    public ARIMADto? ARIMA { get; set; }
}

public class LinearTrendDto
{
    public List<double> PredictedPrices { get; set; } = new();
    public double Slope { get; set; }
    public double Intercept { get; set; }
    public double R2Score { get; set; }
    public string Confidence { get; set; } = string.Empty;
}

public class MovingAverageSignalDto
{
    public string Signal { get; set; } = string.Empty;
    public double Momentum { get; set; }
    public double MA5 { get; set; }
    public double MA20 { get; set; }
}

public class ARIMADto
{
    public List<double> PredictedPrices { get; set; } = new();
    public string Confidence { get; set; } = string.Empty;
} 