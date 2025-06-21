using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Application.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartBIST.Infrastructure.Services;

public class DataMiningAnalysisService : IDataMiningAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataMiningAnalysisService> _logger;
    private readonly string _baseUrl;

    public DataMiningAnalysisService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<DataMiningAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PythonApi:BaseUrl"] ?? "http://localhost:5001";
        
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Increased timeout for complex analysis
    }

    public async Task<DataMiningAnalysisResultDto> GetDataMiningAnalysisAsync(string symbol, int periodDays = 90, bool includePredictions = true)
    {
        try
        {
            var url = $"{_baseUrl}/data-mining-analysis/{symbol}?period_days={periodDays}&predictions={includePredictions.ToString().ToLower()}";
            
            _logger.LogInformation("Requesting data mining analysis for {Symbol} from {Url}", symbol, url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get data mining analysis for {Symbol}. Status: {StatusCode}, Content: {Content}", 
                    symbol, response.StatusCode, errorContent);
                throw new InvalidOperationException($"Python API returned {response.StatusCode}: {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received data mining analysis response: {Response}", jsonContent);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var pythonResponse = JsonSerializer.Deserialize<PythonDataMiningResponse>(jsonContent, options);
            
            if (pythonResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize data mining analysis response");
            }

            return MapToDto(pythonResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while getting data mining analysis for {Symbol}", symbol);
            throw new InvalidOperationException($"Network error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while getting data mining analysis for {Symbol}", symbol);
            throw new InvalidOperationException($"Request timeout: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data mining analysis for {Symbol}", symbol);
            throw;
        }
    }

    private static DataMiningAnalysisResultDto MapToDto(PythonDataMiningResponse pythonResponse)
    {
        return new DataMiningAnalysisResultDto
        {
            Symbol = pythonResponse.Symbol,
            CurrentPrice = pythonResponse.CurrentPrice,
            AnalysisDate = DateTime.TryParse(pythonResponse.AnalysisDate, out var analysisDate) ? analysisDate : DateTime.Now,
            PeriodDays = pythonResponse.PeriodDays,
            DataPoints = pythonResponse.DataPoints,
            AnalysisType = pythonResponse.AnalysisType,
            ClassicIndicators = pythonResponse.ClassicIndicators,
            AdvancedFeatures = MapAdvancedFeatures(pythonResponse.AdvancedFeatures),
            ChartPatterns = MapChartPatterns(pythonResponse.ChartPatterns),
            Anomalies = MapAnomalies(pythonResponse.Anomalies),
            Clustering = MapClustering(pythonResponse.Clustering),
            StatisticalTests = MapStatisticalTests(pythonResponse.StatisticalTests),
            RiskAnalysis = MapRiskAnalysis(pythonResponse.RiskAnalysis),
            AdvancedTechnical = MapAdvancedTechnical(pythonResponse.AdvancedTechnical),
            Predictions = pythonResponse.Predictions != null ? MapPredictions(pythonResponse.Predictions) : null,
            Signals = new TechnicalSignalsDto
            {
                IndividualSignals = pythonResponse.Signals.IndividualSignals,
                OverallSignal = pythonResponse.Signals.OverallSignal,
                SignalStrength = pythonResponse.Signals.SignalStrength,
                BuySignals = pythonResponse.Signals.BuySignals,
                SellSignals = pythonResponse.Signals.SellSignals,
                NeutralSignals = pythonResponse.Signals.NeutralSignals
            },
            PriceHistory = pythonResponse.PriceHistory.Select(p => new PriceDataDto
            {
                Date = DateTime.TryParse(p.Date, out var priceDate) ? priceDate : DateTime.MinValue,
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            }).ToList()
        };
    }

    private static AdvancedFeaturesDto MapAdvancedFeatures(Dictionary<string, object> features)
    {
        return new AdvancedFeaturesDto
        {
            PriceVolatility = GetDoubleValue(features, "price_volatility"),
            PriceMean = GetDoubleValue(features, "price_mean"),
            PriceSkewness = GetDoubleValue(features, "price_skewness"),
            PriceKurtosis = GetDoubleValue(features, "price_kurtosis"),
            VolumeMean = GetDoubleValue(features, "volume_mean"),
            VolumeVolatility = GetDoubleValue(features, "volume_volatility"),
            VolumeTrend = GetDoubleValue(features, "volume_trend"),
            PriceVolumeCorrelation = GetDoubleValue(features, "price_volume_correlation"),
            Momentum1D = GetDoubleValue(features, "momentum_1d"),
            Momentum5D = GetDoubleValue(features, "momentum_5d"),
            Momentum20D = GetDoubleValue(features, "momentum_20d"),
            TrendSlope = GetDoubleValue(features, "trend_slope"),
            TrendRSquared = GetDoubleValue(features, "trend_r_squared"),
            TrendPValue = GetDoubleValue(features, "trend_p_value"),
            HLSpreadMean = GetDoubleValue(features, "hl_spread_mean"),
            HLSpreadVolatility = GetDoubleValue(features, "hl_spread_volatility"),
            PricePosition = GetDoubleValue(features, "price_position"),
            SMA5SMA20Ratio = GetDoubleValue(features, "sma5_sma20_ratio")
        };
    }

    private static ChartPatternsDto MapChartPatterns(Dictionary<string, object> patterns)
    {
        return new ChartPatternsDto
        {
            ResistanceLevels = GetDoubleListValue(patterns, "resistance_levels"),
            SupportLevels = GetDoubleListValue(patterns, "support_levels"),
            LastResistance = GetNullableDoubleValue(patterns, "last_resistance"),
            LastSupport = GetNullableDoubleValue(patterns, "last_support"),
            DoubleTop = GetNullableBoolValue(patterns, "double_top"),
            DoubleTopLevel = GetNullableDoubleValue(patterns, "double_top_level"),
            DoubleBottom = GetNullableBoolValue(patterns, "double_bottom"),
            DoubleBottomLevel = GetNullableDoubleValue(patterns, "double_bottom_level"),
            ChannelWidth = GetNullableDoubleValue(patterns, "channel_width"),
            InChannel = GetNullableBoolValue(patterns, "in_channel")
        };
    }

    private static AnomaliesDto MapAnomalies(Dictionary<string, object> anomalies)
    {
        return new AnomaliesDto
        {
            TotalAnomalies = GetIntValue(anomalies, "total_anomalies"),
            AnomalyRatio = GetDoubleValue(anomalies, "anomaly_ratio"),
            RecentAnomalies = GetBoolValue(anomalies, "recent_anomalies"),
            AnomalyScore = GetDoubleValue(anomalies, "anomaly_score"),
            StatisticalAnomalies = GetIntValue(anomalies, "statistical_anomalies"),
            VolumeAnomalies = GetIntValue(anomalies, "volume_anomalies")
        };
    }

    private static ClusteringDto MapClustering(Dictionary<string, object> clustering)
    {
        var clusterStats = new Dictionary<string, ClusterStatDto>();
        if (clustering.TryGetValue("cluster_statistics", out var statsObj) && statsObj is JsonElement statsElement)
        {
            foreach (var prop in statsElement.EnumerateObject())
            {
                var statDict = JsonSerializer.Deserialize<Dictionary<string, object>>(prop.Value.GetRawText());
                if (statDict != null)
                {
                    clusterStats[prop.Name] = new ClusterStatDto
                    {
                        Size = GetIntValue(statDict, "size"),
                        MeanReturn = GetDoubleValue(statDict, "mean_return"),
                        MeanVolumeChange = GetDoubleValue(statDict, "mean_volume_change"),
                        Volatility = GetDoubleValue(statDict, "volatility")
                    };
                }
            }
        }

        return new ClusteringDto
        {
            ClusterCenters = GetDoubleListListValue(clustering, "cluster_centers"),
            ClusterLabels = GetIntListValue(clustering, "cluster_labels"),
            Inertia = GetDoubleValue(clustering, "inertia"),
            ClusterStatistics = clusterStats,
            CurrentCluster = GetNullableIntValue(clustering, "current_cluster")
        };
    }

    private static StatisticalTestsDto MapStatisticalTests(Dictionary<string, object> tests)
    {
        return new StatisticalTestsDto
        {
            ADFTest = GetNestedObject(tests, "adf_test", dict => new ADFTestDto
            {
                Statistic = GetDoubleValue(dict, "statistic"),
                PValue = GetDoubleValue(dict, "p_value"),
                IsStationary = GetBoolValue(dict, "is_stationary"),
                CriticalValues = GetDictionaryDoubleValue(dict, "critical_values")
            }),
            NormalityTest = GetNestedObject(tests, "normality_test", dict => new NormalityTestDto
            {
                Statistic = GetDoubleValue(dict, "statistic"),
                PValue = GetDoubleValue(dict, "p_value"),
                IsNormal = GetBoolValue(dict, "is_normal")
            }),
            Autocorrelation = GetNestedObject(tests, "autocorrelation", dict => new AutocorrelationDto
            {
                Lag1 = GetDoubleValue(dict, "lag_1"),
                Lag5 = GetDoubleValue(dict, "lag_5")
            }),
            PriceVolumeCorrelation = GetNestedObject(tests, "price_volume_correlation", dict => new PriceVolumeCorrelationDto
            {
                Correlation = GetDoubleValue(dict, "correlation"),
                PValue = GetDoubleValue(dict, "p_value"),
                IsSignificant = GetBoolValue(dict, "is_significant")
            })
        };
    }

    private static RiskAnalysisDto MapRiskAnalysis(Dictionary<string, object> risk)
    {
        return new RiskAnalysisDto
        {
            ValueAtRisk = GetNestedObject(risk, "value_at_risk", dict => new ValueAtRiskDto
            {
                VAR95 = GetDoubleValue(dict, "var_95"),
                VAR99 = GetDoubleValue(dict, "var_99"),
                ExpectedShortfall95 = GetDoubleValue(dict, "expected_shortfall_95")
            }),
            Volatility = GetNestedObject(risk, "volatility", dict => new VolatilityDto
            {
                Daily = GetDoubleValue(dict, "daily"),
                Annualized = GetDoubleValue(dict, "annualized"),
                Rolling30D = GetDoubleValue(dict, "rolling_30d")
            }),
            SharpeRatio = GetNullableDoubleValue(risk, "sharpe_ratio"),
            MaxDrawdown = GetNullableDoubleValue(risk, "max_drawdown"),
            Beta = GetNullableDoubleValue(risk, "beta")
        };
    }

    private static AdvancedTechnicalDto MapAdvancedTechnical(Dictionary<string, object> advanced)
    {
        return new AdvancedTechnicalDto
        {
            Fibonacci = GetNestedObject(advanced, "fibonacci", dict => new FibonacciDto
            {
                High = GetDoubleValue(dict, "high"),
                Low = GetDoubleValue(dict, "low"),
                Levels = GetDictionaryDoubleValue(dict, "levels")
            }),
            PivotPoints = GetNestedObject(advanced, "pivot_points", dict => new PivotPointsDto
            {
                Pivot = GetDoubleValue(dict, "pivot"),
                Resistance1 = GetDoubleValue(dict, "resistance_1"),
                Support1 = GetDoubleValue(dict, "support_1"),
                Resistance2 = GetDoubleValue(dict, "resistance_2"),
                Support2 = GetDoubleValue(dict, "support_2")
            }),
            Ichimoku = GetNestedObject(advanced, "ichimoku", dict => new IchimokuDto
            {
                TenkanSen = GetDoubleValue(dict, "tenkan_sen"),
                KijunSen = GetDoubleValue(dict, "kijun_sen"),
                CloudTop = GetDoubleValue(dict, "cloud_top"),
                CloudBottom = GetDoubleValue(dict, "cloud_bottom"),
                PricePosition = GetStringValue(dict, "price_position")
            })
        };
    }

    private static PredictionsDto MapPredictions(Dictionary<string, object> predictions)
    {
        return new PredictionsDto
        {
            LinearTrend = GetNestedObject(predictions, "linear_trend", dict => new LinearTrendDto
            {
                PredictedPrices = GetDoubleListValue(dict, "predicted_prices"),
                Slope = GetDoubleValue(dict, "slope"),
                Intercept = GetDoubleValue(dict, "intercept"),
                R2Score = GetDoubleValue(dict, "r2_score"),
                Confidence = GetStringValue(dict, "confidence")
            }),
            MovingAverageSignal = GetNestedObject(predictions, "moving_average_signal", dict => new MovingAverageSignalDto
            {
                Signal = GetStringValue(dict, "signal"),
                Momentum = GetDoubleValue(dict, "momentum"),
                MA5 = GetDoubleValue(dict, "ma5"),
                MA20 = GetDoubleValue(dict, "ma20")
            }),
            ARIMA = GetNestedObject(predictions, "arima", dict => new ARIMADto
            {
                PredictedPrices = GetDoubleListValue(dict, "predicted_prices"),
                Confidence = GetStringValue(dict, "confidence")
            })
        };
    }

    // Helper methods for safe value extraction
    private static double GetDoubleValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                double d => d,
                float f => f,
                decimal dec => (double)dec,
                int i => i,
                long l => l,
                JsonElement json => json.GetDouble(),
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => 0.0
            };
        }
        return 0.0;
    }

    private static int GetIntValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                float f => (int)f,
                decimal dec => (int)dec,
                JsonElement json => json.GetInt32(),
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => 0
            };
        }
        return 0;
    }

    private static bool GetBoolValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                bool b => b,
                JsonElement json => json.GetBoolean(),
                string s when bool.TryParse(s, out var parsed) => parsed,
                _ => false
            };
        }
        return false;
    }

    private static string GetStringValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static double? GetNullableDoubleValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return GetDoubleValue(dict, key);
        }
        return null;
    }

    private static int? GetNullableIntValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return GetIntValue(dict, key);
        }
        return null;
    }

    private static bool? GetNullableBoolValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return GetBoolValue(dict, key);
        }
        return null;
    }

    private static List<double> GetDoubleListValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            return json.EnumerateArray().Select(x => x.GetDouble()).ToList();
        }
        return new List<double>();
    }

    private static List<int> GetIntListValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            return json.EnumerateArray().Select(x => x.GetInt32()).ToList();
        }
        return new List<int>();
    }

    private static List<List<double>> GetDoubleListListValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            return json.EnumerateArray()
                .Select(arr => arr.EnumerateArray().Select(x => x.GetDouble()).ToList())
                .ToList();
        }
        return new List<List<double>>();
    }

    private static Dictionary<string, double> GetDictionaryDoubleValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, double>();
            foreach (var prop in json.EnumerateObject())
            {
                result[prop.Name] = prop.Value.GetDouble();
            }
            return result;
        }
        return new Dictionary<string, double>();
    }

    private static T? GetNestedObject<T>(Dictionary<string, object> dict, string key, Func<Dictionary<string, object>, T> mapper) where T : class
    {
        if (dict.TryGetValue(key, out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json.GetRawText());
            return nestedDict != null ? mapper(nestedDict) : null;
        }
        return null;
    }
}

// Python API Response Models for Data Mining
public class PythonDataMiningResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public string AnalysisDate { get; set; } = string.Empty;
    public int PeriodDays { get; set; }
    public int DataPoints { get; set; }
    public string AnalysisType { get; set; } = string.Empty;
    public Dictionary<string, object> ClassicIndicators { get; set; } = new();
    public Dictionary<string, object> AdvancedFeatures { get; set; } = new();
    public Dictionary<string, object> ChartPatterns { get; set; } = new();
    public Dictionary<string, object> Anomalies { get; set; } = new();
    public Dictionary<string, object> Clustering { get; set; } = new();
    public Dictionary<string, object> StatisticalTests { get; set; } = new();
    public Dictionary<string, object> RiskAnalysis { get; set; } = new();
    public Dictionary<string, object> AdvancedTechnical { get; set; } = new();
    public Dictionary<string, object>? Predictions { get; set; }
    public PythonSignalsResponse Signals { get; set; } = new();
    public List<PythonPriceData> PriceHistory { get; set; } = new();
}