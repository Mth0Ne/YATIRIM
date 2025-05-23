using SmartBIST.Application.DTOs;
using SmartBIST.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SmartBIST.Application.Services;

public interface ITechnicalIndicatorService
{
    Task<Dictionary<string, object>> CalculateIndicatorAsync(int stockId, string indicator, Dictionary<string, string> parameters);
    Task<Dictionary<string, object>> CalculateAllIndicatorsAsync(int stockId, int period = 30);
    Task<LegacyTechnicalAnalysisResultDto> GetFullTechnicalAnalysisAsync(int stockId, int period = 90);
}

public class TechnicalIndicatorService : ITechnicalIndicatorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TechnicalIndicatorService> _logger;

    public TechnicalIndicatorService(IUnitOfWork unitOfWork, ILogger<TechnicalIndicatorService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> CalculateIndicatorAsync(int stockId, string indicator, Dictionary<string, string> parameters)
    {
        try
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-200); // Yeterli veri için 200 gün
            
            var priceHistory = await _unitOfWork.StockPriceHistories
                .GetPriceHistoryByDateRangeAsync(stockId, startDate, endDate);
            
            var prices = priceHistory.OrderBy(p => p.Date).ToList();
            
            if (!prices.Any())
            {
                return new Dictionary<string, object> { ["error"] = "Insufficient price data" };
            }

            return indicator.ToUpper() switch
            {
                "SMA" => CalculateSMA(prices, GetParameter(parameters, "period", 20)),
                "EMA" => CalculateEMA(prices, GetParameter(parameters, "period", 20)),
                "RSI" => CalculateRSI(prices, GetParameter(parameters, "period", 14)),
                "MACD" => CalculateMACD(prices, GetParameter(parameters, "fast", 12), GetParameter(parameters, "slow", 26), GetParameter(parameters, "signal", 9)),
                "BOLLINGER" => CalculateBollingerBands(prices, GetParameter(parameters, "period", 20), GetParameterDouble(parameters, "stddev", 2.0)),
                "STOCHASTIC" => CalculateStochastic(prices, GetParameter(parameters, "k_period", 14), GetParameter(parameters, "d_period", 3)),
                "WILLIAMS_R" => CalculateWilliamsR(prices, GetParameter(parameters, "period", 14)),
                "CCI" => CalculateCCI(prices, GetParameter(parameters, "period", 20)),
                _ => new Dictionary<string, object> { ["error"] = "Unsupported indicator" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating technical indicator {Indicator} for stock {StockId}", indicator, stockId);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    public async Task<Dictionary<string, object>> CalculateAllIndicatorsAsync(int stockId, int period = 30)
    {
        var result = new Dictionary<string, object>();
        
        var indicators = new Dictionary<string, Dictionary<string, string>>
        {
            { "SMA", new() { ["period"] = "20" } },
            { "EMA", new() { ["period"] = "20" } },
            { "RSI", new() { ["period"] = "14" } },
            { "MACD", new() { ["fast"] = "12", ["slow"] = "26", ["signal"] = "9" } },
            { "BOLLINGER", new() { ["period"] = "20", ["stddev"] = "2.0" } },
            { "STOCHASTIC", new() { ["k_period"] = "14", ["d_period"] = "3" } },
            { "WILLIAMS_R", new() { ["period"] = "14" } },
            { "CCI", new() { ["period"] = "20" } }
        };

        foreach (var indicator in indicators)
        {
            var calculation = await CalculateIndicatorAsync(stockId, indicator.Key, indicator.Value);
            result[indicator.Key.ToLower()] = calculation;
        }

        return result;
    }

    public async Task<LegacyTechnicalAnalysisResultDto> GetFullTechnicalAnalysisAsync(int stockId, int period = 90)    {        var stock = await _unitOfWork.Stocks.GetByIdAsync(stockId);        if (stock == null)        {            throw new ArgumentException($"Stock with ID {stockId} not found");        }        var indicators = await CalculateAllIndicatorsAsync(stockId, period);        var signals = GenerateTradingSignals(indicators);                return new LegacyTechnicalAnalysisResultDto        {            StockId = stockId,            StockSymbol = stock.Symbol,            StockName = stock.Name,            CurrentPrice = stock.CurrentPrice,            AnalysisDate = DateTime.Now,            Indicators = indicators,            Signals = signals,            OverallSignal = CalculateOverallSignal(signals),            SignalStrength = CalculateSignalStrength(signals)        };    }

    #region Helper Methods

    private int GetParameter(Dictionary<string, string> parameters, string key, int defaultValue)
    {
        return parameters != null && parameters.TryGetValue(key, out var value) && int.TryParse(value, out var result) 
            ? result : defaultValue;
    }

    private double GetParameterDouble(Dictionary<string, string> parameters, string key, double defaultValue)
    {
        return parameters != null && parameters.TryGetValue(key, out var value) && double.TryParse(value, out var result) 
            ? result : defaultValue;
    }

    #endregion

    #region Technical Indicator Calculations

    private Dictionary<string, object> CalculateSMA(List<Core.Entities.StockPriceHistory> prices, int period)
    {
        var closes = prices.Select(p => (double)p.Close).ToArray();
        var smaValues = new List<double>();
        var dates = new List<string>();

        for (int i = period - 1; i < closes.Length; i++)
        {
            var sma = closes.Skip(i - period + 1).Take(period).Average();
            smaValues.Add(Math.Round(sma, 2));
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        return new Dictionary<string, object>
        {
            ["values"] = smaValues,
            ["dates"] = dates,
            ["current"] = smaValues.LastOrDefault(),
            ["period"] = period
        };
    }

    private Dictionary<string, object> CalculateEMA(List<Core.Entities.StockPriceHistory> prices, int period)
    {
        var closes = prices.Select(p => (double)p.Close).ToArray();
        var emaValues = new List<double>();
        var dates = new List<string>();
        
        if (closes.Length < period) 
            return new Dictionary<string, object> { ["error"] = "Insufficient data" };

        var multiplier = 2.0 / (period + 1);
        var ema = closes.Take(period).Average(); // İlk EMA = SMA

        for (int i = period - 1; i < closes.Length; i++)
        {
            if (i == period - 1)
            {
                emaValues.Add(Math.Round(ema, 2));
            }
            else
            {
                ema = (closes[i] * multiplier) + (ema * (1 - multiplier));
                emaValues.Add(Math.Round(ema, 2));
            }
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        return new Dictionary<string, object>
        {
            ["values"] = emaValues,
            ["dates"] = dates,
            ["current"] = emaValues.LastOrDefault(),
            ["period"] = period
        };
    }

    private Dictionary<string, object> CalculateRSI(List<Core.Entities.StockPriceHistory> prices, int period)
    {
        var closes = prices.Select(p => (double)p.Close).ToArray();
        var rsiValues = new List<double>();
        var dates = new List<string>();

        if (closes.Length < period + 1) 
            return new Dictionary<string, object> { ["error"] = "Insufficient data" };

        var gains = new List<double>();
        var losses = new List<double>();

        // Fiyat değişimlerini hesapla
        for (int i = 1; i < closes.Length; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        // İlk RSI hesaplaması (SMA kullanarak)
        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        for (int i = period; i < gains.Count; i++)
        {
            if (i == period)
            {
                // İlk RSI
                var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                var rsi = 100 - (100 / (1 + rs));
                rsiValues.Add(Math.Round(rsi, 2));
            }
            else
            {
                // Smoothed RSI
                avgGain = ((avgGain * (period - 1)) + gains[i]) / period;
                avgLoss = ((avgLoss * (period - 1)) + losses[i]) / period;
                
                var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                var rsi = 100 - (100 / (1 + rs));
                rsiValues.Add(Math.Round(rsi, 2));
            }
            dates.Add(prices[i + 1].Date.ToString("yyyy-MM-dd"));
        }

        return new Dictionary<string, object>
        {
            ["values"] = rsiValues,
            ["dates"] = dates,
            ["current"] = rsiValues.LastOrDefault(),
            ["period"] = period,
            ["signal"] = GetRSISignal(rsiValues.LastOrDefault())
        };
    }

    private Dictionary<string, object> CalculateMACD(List<Core.Entities.StockPriceHistory> prices, int fastPeriod, int slowPeriod, int signalPeriod)
    {
        var fastEMA = CalculateEMA(prices, fastPeriod);
        var slowEMA = CalculateEMA(prices, slowPeriod);
        
        if (fastEMA.ContainsKey("error") || slowEMA.ContainsKey("error"))
            return new Dictionary<string, object> { ["error"] = "Insufficient data for MACD" };

        var fastValues = (List<double>)fastEMA["values"];
        var slowValues = (List<double>)slowEMA["values"];
        var dates = (List<string>)fastEMA["dates"];

        // MACD Line = Fast EMA - Slow EMA
        var macdLine = new List<double>();
        var startIndex = slowValues.Count - fastValues.Count;
        
        for (int i = startIndex; i < fastValues.Count; i++)
        {
            macdLine.Add(Math.Round(fastValues[i] - slowValues[i - startIndex], 4));
        }

        // Signal Line = EMA of MACD Line
        var signalLine = CalculateEMAFromValues(macdLine, signalPeriod);
        
        // Histogram = MACD Line - Signal Line
        var histogram = new List<double>();
        for (int i = signalPeriod - 1; i < macdLine.Count; i++)
        {
            histogram.Add(Math.Round(macdLine[i] - signalLine[i - signalPeriod + 1], 4));
        }

        return new Dictionary<string, object>
        {
            ["macd_line"] = macdLine,
            ["signal_line"] = signalLine,
            ["histogram"] = histogram,
            ["dates"] = dates.Skip(dates.Count - macdLine.Count).ToList(),
            ["current_macd"] = macdLine.LastOrDefault(),
            ["current_signal"] = signalLine.LastOrDefault(),
            ["current_histogram"] = histogram.LastOrDefault(),
            ["signal"] = GetMACDSignal(macdLine.LastOrDefault(), signalLine.LastOrDefault(), histogram.LastOrDefault())
        };
    }

    private Dictionary<string, object> CalculateBollingerBands(List<Core.Entities.StockPriceHistory> prices, int period, double stdDev)
    {
        var closes = prices.Select(p => (double)p.Close).ToArray();
        var upperBand = new List<double>();
        var lowerBand = new List<double>();
        var middleBand = new List<double>();
        var dates = new List<string>();

        for (int i = period - 1; i < closes.Length; i++)
        {
            var subset = closes.Skip(i - period + 1).Take(period);
            var sma = subset.Average();
            var variance = subset.Select(x => Math.Pow(x - sma, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            middleBand.Add(Math.Round(sma, 2));
            upperBand.Add(Math.Round(sma + (stdDev * standardDeviation), 2));
            lowerBand.Add(Math.Round(sma - (stdDev * standardDeviation), 2));
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        var currentPrice = (double)prices.Last().Close;
        return new Dictionary<string, object>
        {
            ["upper_band"] = upperBand,
            ["middle_band"] = middleBand,
            ["lower_band"] = lowerBand,
            ["dates"] = dates,
            ["current_upper"] = upperBand.LastOrDefault(),
            ["current_middle"] = middleBand.LastOrDefault(),
            ["current_lower"] = lowerBand.LastOrDefault(),
            ["period"] = period,
            ["std_dev"] = stdDev,
            ["signal"] = GetBollingerSignal(currentPrice, upperBand.LastOrDefault(), lowerBand.LastOrDefault())
        };
    }

    private Dictionary<string, object> CalculateStochastic(List<Core.Entities.StockPriceHistory> prices, int kPeriod, int dPeriod)
    {
        var kValues = new List<double>();
        var dValues = new List<double>();
        var dates = new List<string>();

        for (int i = kPeriod - 1; i < prices.Count; i++)
        {
            var period = prices.Skip(i - kPeriod + 1).Take(kPeriod);
            var high = period.Max(p => (double)p.High);
            var low = period.Min(p => (double)p.Low);
            var close = (double)prices[i].Close;

            var k = ((close - low) / (high - low)) * 100;
            kValues.Add(Math.Round(k, 2));
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        // %D = SMA of %K
        for (int i = dPeriod - 1; i < kValues.Count; i++)
        {
            var d = kValues.Skip(i - dPeriod + 1).Take(dPeriod).Average();
            dValues.Add(Math.Round(d, 2));
        }

        return new Dictionary<string, object>
        {
            ["k_values"] = kValues,
            ["d_values"] = dValues,
            ["dates"] = dates,
            ["current_k"] = kValues.LastOrDefault(),
            ["current_d"] = dValues.LastOrDefault(),
            ["signal"] = GetStochasticSignal(kValues.LastOrDefault(), dValues.LastOrDefault())
        };
    }

    private Dictionary<string, object> CalculateWilliamsR(List<Core.Entities.StockPriceHistory> prices, int period)
    {
        var williamsR = new List<double>();
        var dates = new List<string>();

        for (int i = period - 1; i < prices.Count; i++)
        {
            var periodPrices = prices.Skip(i - period + 1).Take(period);
            var high = periodPrices.Max(p => (double)p.High);
            var low = periodPrices.Min(p => (double)p.Low);
            var close = (double)prices[i].Close;

            var wr = ((high - close) / (high - low)) * -100;
            williamsR.Add(Math.Round(wr, 2));
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        return new Dictionary<string, object>
        {
            ["values"] = williamsR,
            ["dates"] = dates,
            ["current"] = williamsR.LastOrDefault(),
            ["signal"] = GetWilliamsRSignal(williamsR.LastOrDefault())
        };
    }

    private Dictionary<string, object> CalculateCCI(List<Core.Entities.StockPriceHistory> prices, int period)
    {
        var cciValues = new List<double>();
        var dates = new List<string>();

        for (int i = period - 1; i < prices.Count; i++)
        {
            var periodPrices = prices.Skip(i - period + 1).Take(period);
            
            // Typical Price = (High + Low + Close) / 3
            var typicalPrices = periodPrices.Select(p => ((double)p.High + (double)p.Low + (double)p.Close) / 3).ToList();
            var smaTP = typicalPrices.Average();
            
            // Mean Deviation
            var meanDeviation = typicalPrices.Select(tp => Math.Abs(tp - smaTP)).Average();
            
            var currentTP = typicalPrices.Last();
            var cci = (currentTP - smaTP) / (0.015 * meanDeviation);
            
            cciValues.Add(Math.Round(cci, 2));
            dates.Add(prices[i].Date.ToString("yyyy-MM-dd"));
        }

        return new Dictionary<string, object>
        {
            ["values"] = cciValues,
            ["dates"] = dates,
            ["current"] = cciValues.LastOrDefault(),
            ["signal"] = GetCCISignal(cciValues.LastOrDefault())
        };
    }

    private List<double> CalculateEMAFromValues(List<double> values, int period)
    {
        var result = new List<double>();
        var multiplier = 2.0 / (period + 1);
        var ema = values.Take(period).Average();

        for (int i = period - 1; i < values.Count; i++)
        {
            if (i == period - 1)
            {
                result.Add(ema);
            }
            else
            {
                ema = (values[i] * multiplier) + (ema * (1 - multiplier));
                result.Add(ema);
            }
        }

        return result;
    }

    #endregion

    #region Signal Generation

    private string GetRSISignal(double rsi)
    {
        return rsi switch
        {
            > 70 => "SELL",
            < 30 => "BUY",
            _ => "NEUTRAL"
        };
    }

    private string GetMACDSignal(double macd, double signal, double histogram)
    {
        if (macd > signal && histogram > 0) return "BUY";
        if (macd < signal && histogram < 0) return "SELL";
        return "NEUTRAL";
    }

    private string GetBollingerSignal(double price, double upper, double lower)
    {
        if (price > upper) return "SELL";
        if (price < lower) return "BUY";
        return "NEUTRAL";
    }

    private string GetStochasticSignal(double k, double d)
    {
        if (k > 80 && d > 80) return "SELL";
        if (k < 20 && d < 20) return "BUY";
        return "NEUTRAL";
    }

    private string GetWilliamsRSignal(double wr)
    {
        return wr switch
        {
            > -20 => "SELL",
            < -80 => "BUY",
            _ => "NEUTRAL"
        };
    }

    private string GetCCISignal(double cci)
    {
        return cci switch
        {
            > 100 => "SELL",
            < -100 => "BUY",
            _ => "NEUTRAL"
        };
    }

    private Dictionary<string, object> GenerateTradingSignals(Dictionary<string, object> indicators)
    {
        var signals = new Dictionary<string, string>();
        
        foreach (var indicator in indicators)
        {
            if (indicator.Value is Dictionary<string, object> indicatorData && 
                indicatorData.TryGetValue("signal", out var signal))
            {
                signals[indicator.Key] = signal.ToString();
            }
        }

        return new Dictionary<string, object>
        {
            ["individual_signals"] = signals,
            ["buy_signals"] = signals.Values.Count(s => s == "BUY"),
            ["sell_signals"] = signals.Values.Count(s => s == "SELL"),
            ["neutral_signals"] = signals.Values.Count(s => s == "NEUTRAL")
        };
    }

    private string CalculateOverallSignal(Dictionary<string, object> signals)
    {
        var individualSignals = (Dictionary<string, string>)signals["individual_signals"];
        var buyCount = (int)signals["buy_signals"];
        var sellCount = (int)signals["sell_signals"];

        if (buyCount > sellCount) return "BUY";
        if (sellCount > buyCount) return "SELL";
        return "NEUTRAL";
    }

    private double CalculateSignalStrength(Dictionary<string, object> signals)
    {
        var buyCount = (int)signals["buy_signals"];
        var sellCount = (int)signals["sell_signals"];
        var totalSignals = buyCount + sellCount + (int)signals["neutral_signals"];

        var strongestSignalCount = Math.Max(buyCount, sellCount);
        return totalSignals > 0 ? (double)strongestSignalCount / totalSignals : 0;
    }

    #endregion
} 