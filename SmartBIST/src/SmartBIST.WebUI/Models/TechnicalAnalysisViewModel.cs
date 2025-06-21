using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBIST.Application.DTOs;

namespace SmartBIST.WebUI.Models
{
    public class TechnicalAnalysisViewModel
    {
        // Form properties
        public List<SelectListItem> AvailableStocks { get; set; } = new();
        public string? Symbol { get; set; }
        public int Period { get; set; } = 90;
        public bool EnableDataMining { get; set; } = false;
        public bool IncludePredictions { get; set; } = true;
        
        // Data properties
        public TechnicalAnalysisResultDto? TechnicalAnalysis { get; set; }
        public DataMiningAnalysisResultDto? DataMiningAnalysis { get; set; }
        public PriceHistoryResultDto? PriceHistory { get; set; }
        
        // Analysis type
        public string AnalysisType => EnableDataMining ? "data_mining" : "classical";
        
        // State properties
        public string? ErrorMessage { get; set; }
        public bool IsDataLoaded { get; set; }
        
        // Helper properties for the view
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasData => IsDataLoaded && (TechnicalAnalysis != null || DataMiningAnalysis != null) && PriceHistory != null;
        public bool IsDataMiningAnalysis => EnableDataMining && DataMiningAnalysis != null;
        public bool IsClassicalAnalysis => !EnableDataMining && TechnicalAnalysis != null;
        
        // Advanced features availability
        public bool HasAdvancedFeatures => DataMiningAnalysis?.AdvancedFeatures != null;
        public bool HasChartPatterns => DataMiningAnalysis?.ChartPatterns != null;
        public bool HasAnomalies => DataMiningAnalysis?.Anomalies != null;
        public bool HasClustering => DataMiningAnalysis?.Clustering != null;
        public bool HasStatisticalTests => DataMiningAnalysis?.StatisticalTests != null;
        public bool HasRiskAnalysis => DataMiningAnalysis?.RiskAnalysis != null;
        public bool HasAdvancedTechnical => DataMiningAnalysis?.AdvancedTechnical != null;
        public bool HasPredictions => DataMiningAnalysis?.Predictions != null;
        
        // Signal strength indicator
        public string SignalStrengthClass
        {
            get
            {
                var strength = DataMiningAnalysis?.Signals?.SignalStrength ?? TechnicalAnalysis?.Signals?.SignalStrength ?? 0;
                return strength switch
                {
                    >= 0.8 => "signal-very-strong",
                    >= 0.6 => "signal-strong", 
                    >= 0.4 => "signal-moderate",
                    >= 0.2 => "signal-weak",
                    _ => "signal-very-weak"
                };
            }
        }
        
        // Overall signal color
        public string OverallSignalClass
        {
            get
            {
                var signal = DataMiningAnalysis?.Signals?.OverallSignal ?? TechnicalAnalysis?.Signals?.OverallSignal ?? "NEUTRAL";
                return signal switch
                {
                    "BUY" => "signal-buy",
                    "SELL" => "signal-sell",
                    _ => "signal-neutral"
                };
            }
        }
    }
} 