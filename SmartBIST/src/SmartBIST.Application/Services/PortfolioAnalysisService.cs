using SmartBIST.Application.DTOs;
using SmartBIST.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SmartBIST.Application.Services;

public interface IPortfolioAnalysisService
{
    Task<Dictionary<string, object>> GetPortfolioAnalysisAsync(int portfolioId, string userId);
    Task<Dictionary<string, object>> GetPortfolioRecommendationsAsync(int portfolioId, string userId);
    Task<PortfolioMetricsDto> CalculatePortfolioMetricsAsync(int portfolioId, string userId);
    Task<Dictionary<string, object>> GetRiskAnalysisAsync(int portfolioId, string userId);
    Task<Dictionary<string, object>> GetDiversificationAnalysisAsync(int portfolioId, string userId);
}

public class PortfolioAnalysisService : IPortfolioAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PortfolioAnalysisService> _logger;

    public PortfolioAnalysisService(IUnitOfWork unitOfWork, ILogger<PortfolioAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> GetPortfolioAnalysisAsync(int portfolioId, string userId)
    {
        try
        {
            var portfolio = await ValidatePortfolioOwnership(portfolioId, userId);
            var portfolioWithItems = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
            
            if (portfolioWithItems == null || !portfolioWithItems.Items.Any())
            {
                return new Dictionary<string, object> { ["error"] = "Portfolio is empty or not found" };
            }

            var metrics = await CalculatePortfolioMetricsAsync(portfolioId, userId);
            var riskAnalysis = await GetRiskAnalysisAsync(portfolioId, userId);
            var diversificationAnalysis = await GetDiversificationAnalysisAsync(portfolioId, userId);

            return new Dictionary<string, object>
            {
                ["total_value"] = metrics.TotalValue,
                ["total_cost"] = metrics.TotalCost,
                ["total_profit_loss"] = metrics.TotalProfitLoss,
                ["total_profit_loss_percentage"] = metrics.TotalProfitLossPercentage,
                ["daily_return"] = metrics.DailyReturn,
                ["expected_annual_return"] = metrics.ExpectedAnnualReturn,
                ["portfolio_beta"] = metrics.PortfolioBeta,
                ["sharpe_ratio"] = metrics.SharpeRatio,
                ["sortino_ratio"] = metrics.SortinoRatio,
                ["maximum_drawdown"] = metrics.MaximumDrawdown,
                ["volatility"] = riskAnalysis["volatility"],
                ["var_95"] = riskAnalysis["var_95"],
                ["risk_score"] = riskAnalysis["risk_score"],
                ["diversification_score"] = diversificationAnalysis["diversification_score"],
                ["sector_allocation"] = diversificationAnalysis["sector_allocation"],
                ["concentration_risk"] = diversificationAnalysis["concentration_risk"],
                ["last_updated"] = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio analysis for portfolio {PortfolioId}", portfolioId);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    public async Task<Dictionary<string, object>> GetPortfolioRecommendationsAsync(int portfolioId, string userId)
    {
        try
        {
            var analysis = await GetPortfolioAnalysisAsync(portfolioId, userId);
            if (analysis.ContainsKey("error")) return analysis;

            var recommendations = new List<string>();
            var rebalanceSuggestions = new List<string>();
            var diversificationTips = new List<string>();

            // Risk-based recommendations
            var riskScore = (double)analysis["risk_score"];
            if (riskScore > 0.8)
            {
                recommendations.Add("Portföyünüz yüksek riskli. Daha muhafazakar varlıklara yönelmeyi düşünün.");
                rebalanceSuggestions.Add("Toplam portföyün %20-30'unu devlet tahvili veya altın gibi güvenli varlıklara yönlendirin.");
            }
            else if (riskScore < 0.3)
            {
                recommendations.Add("Portföyünüz çok muhafazakar. Daha yüksek getiri için büyüme hisselerine ağırlık verebilirsiniz.");
            }

            // Diversification-based recommendations
            var diversificationScore = (double)analysis["diversification_score"];
            if (diversificationScore < 0.6)
            {
                diversificationTips.Add("Portföyünüz yeterince çeşitlendirilmemiş. Farklı sektörlerden hisselere yatırım yapın.");
                diversificationTips.Add("En az 5-6 farklı sektörden hisse bulundurmaya çalışın.");
            }

            // Concentration risk recommendations
            var concentrationRisk = (double)analysis["concentration_risk"];
            if (concentrationRisk > 0.4)
            {
                rebalanceSuggestions.Add("Tek bir hissenin portföydeki ağırlığı %40'ı geçiyor. Bu pozisyonu azaltın.");
                diversificationTips.Add("Hiçbir hissenin portföydeki ağırlığı %25'i geçmemesine dikkat edin.");
            }

            // Performance-based recommendations
            var sharpeRatio = (double)analysis["sharpe_ratio"];
            if (sharpeRatio < 0.5)
            {
                recommendations.Add("Risk-ayarlı getiriniz düşük. Portföy kompozisyonunu gözden geçirin.");
            }

            // Volatility recommendations
            var volatility = (double)analysis["volatility"];
            if (volatility > 0.25)
            {
                recommendations.Add("Portföyünüzün volatilitesi yüksek. Daha stabil hisseler eklemeyi düşünün.");
            }

            return new Dictionary<string, object>
            {
                ["general_recommendations"] = recommendations,
                ["rebalance_suggestions"] = rebalanceSuggestions,
                ["diversification_tips"] = diversificationTips,
                ["risk_assessment"] = GetRiskAssessmentText(riskScore),
                ["overall_score"] = CalculateOverallPortfolioScore(analysis),
                ["next_review_date"] = DateTime.Now.AddDays(30),
                ["analysis_date"] = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating portfolio recommendations for portfolio {PortfolioId}", portfolioId);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    public async Task<PortfolioMetricsDto> CalculatePortfolioMetricsAsync(int portfolioId, string userId)
    {
        var portfolio = await ValidatePortfolioOwnership(portfolioId, userId);
        var portfolioWithItems = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);

        if (portfolioWithItems == null || !portfolioWithItems.Items.Any())
        {
            return new PortfolioMetricsDto();
        }

        var totalValue = portfolioWithItems.Items.Sum(item => item.Quantity * item.Stock.CurrentPrice);
        var totalCost = portfolioWithItems.Items.Sum(item => item.Quantity * item.AveragePrice);
        var totalProfitLoss = totalValue - totalCost;
        var totalProfitLossPercentage = totalCost > 0 ? (totalProfitLoss / totalCost) * 100 : 0;

        // Daily return calculation
        var dailyReturn = await CalculateDailyReturn(portfolioWithItems);
        
        // Historical returns for advanced metrics
        var historicalReturns = await CalculateHistoricalReturns(portfolioWithItems);
        
        // Risk-free rate (10-year Turkish government bond yield approximation)
        var riskFreeRate = 0.15; // %15 (example)
        
        var volatility = CalculateVolatility(historicalReturns);
        var expectedAnnualReturn = historicalReturns.Any() ? historicalReturns.Average() * 252 : 0; // 252 trading days
        var sharpeRatio = volatility > 0 ? (expectedAnnualReturn - riskFreeRate) / volatility : 0;
        var sortinoRatio = CalculateSortinoRatio(historicalReturns, riskFreeRate);
        var maxDrawdown = CalculateMaximumDrawdown(historicalReturns);
        var portfolioBeta = await CalculatePortfolioBeta(portfolioWithItems);

        return new PortfolioMetricsDto
        {
            PortfolioId = portfolioId,
            TotalValue = totalValue,
            TotalCost = totalCost,
            TotalProfitLoss = totalProfitLoss,
            TotalProfitLossPercentage = (double)totalProfitLossPercentage,
            DailyReturn = dailyReturn,
            ExpectedAnnualReturn = expectedAnnualReturn,
            Volatility = volatility,
            SharpeRatio = sharpeRatio,
            SortinoRatio = sortinoRatio,
            MaximumDrawdown = maxDrawdown,
            PortfolioBeta = portfolioBeta,
            CalculationDate = DateTime.Now
        };
    }

    public async Task<Dictionary<string, object>> GetRiskAnalysisAsync(int portfolioId, string userId)
    {
        var portfolio = await ValidatePortfolioOwnership(portfolioId, userId);
        var portfolioWithItems = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        
        if (portfolioWithItems == null || !portfolioWithItems.Items.Any())
        {
            return new Dictionary<string, object> { ["error"] = "Portfolio is empty" };
        }

        var historicalReturns = await CalculateHistoricalReturns(portfolioWithItems);
        var volatility = CalculateVolatility(historicalReturns);
        var var95 = CalculateValueAtRisk(historicalReturns, 0.95);
        var var99 = CalculateValueAtRisk(historicalReturns, 0.99);
        
        // Risk score (0-1 scale)
        var riskScore = CalculateRiskScore(volatility, var95);
        
        return new Dictionary<string, object>
        {
            ["volatility"] = volatility,
            ["var_95"] = var95, // 95% confidence level VaR
            ["var_99"] = var99, // 99% confidence level VaR
            ["risk_score"] = riskScore,
            ["risk_level"] = GetRiskLevel(riskScore),
            ["beta"] = await CalculatePortfolioBeta(portfolioWithItems),
            ["tracking_error"] = CalculateTrackingError(historicalReturns),
            ["downside_deviation"] = CalculateDownsideDeviation(historicalReturns, 0)
        };
    }

    public async Task<Dictionary<string, object>> GetDiversificationAnalysisAsync(int portfolioId, string userId)
    {
        var portfolio = await ValidatePortfolioOwnership(portfolioId, userId);
        var portfolioWithItems = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        
        if (portfolioWithItems == null || !portfolioWithItems.Items.Any())
        {
            return new Dictionary<string, object> { ["error"] = "Portfolio is empty" };
        }

        var totalValue = portfolioWithItems.Items.Sum(item => item.Quantity * item.Stock.CurrentPrice);
        
        // Sector allocation analysis
        var sectorAllocation = CalculateSectorAllocation(portfolioWithItems, totalValue);
        
        // Concentration risk (Herfindahl-Hirschman Index)
        var concentrationRisk = CalculateConcentrationRisk(portfolioWithItems, totalValue);
        
        // Diversification score
        var diversificationScore = CalculateDiversificationScore(sectorAllocation, concentrationRisk);
        
        return new Dictionary<string, object>
        {
            ["diversification_score"] = diversificationScore,
            ["concentration_risk"] = concentrationRisk,
            ["sector_allocation"] = sectorAllocation,
            ["number_of_holdings"] = portfolioWithItems.Items.Count,
            ["largest_holding_percentage"] = GetLargestHoldingPercentage(portfolioWithItems, totalValue),
            ["effective_number_of_stocks"] = CalculateEffectiveNumberOfStocks(portfolioWithItems, totalValue)
        };
    }

    #region Helper Methods

    private async Task<Core.Entities.Portfolio> ValidatePortfolioOwnership(int portfolioId, string userId)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("Portfolio not found or access denied");
        }
        return portfolio;
    }

    private Task<double> CalculateDailyReturn(Core.Entities.Portfolio portfolio)
    {
        // Calculate weighted average of stock daily changes
        var totalValue = portfolio.Items.Sum(item => item.Quantity * item.Stock.CurrentPrice);
        if (totalValue == 0) return Task.FromResult(0.0);

        var weightedReturn = 0.0;
        foreach (var item in portfolio.Items)
        {
            var weight = (double)(item.Quantity * item.Stock.CurrentPrice) / (double)totalValue;
            weightedReturn += weight * (double)item.Stock.DailyChangePercentage / 100;
        }

        return Task.FromResult(weightedReturn);
    }

    private async Task<List<double>> CalculateHistoricalReturns(Core.Entities.Portfolio portfolio)
    {
        var returns = new List<double>();
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddDays(-60); // Last 60 days

        try
        {
            var dates = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(date);
                }
            }

            for (int i = 1; i < dates.Count; i++)
            {
                var previousValue = 0.0;
                var currentValue = 0.0;

                foreach (var item in portfolio.Items)
                {
                    var previousPrice = await GetStockPriceOnDate(item.StockId, dates[i - 1]);
                    var currentPrice = await GetStockPriceOnDate(item.StockId, dates[i]);

                    previousValue += (double)item.Quantity * previousPrice;
                    currentValue += (double)item.Quantity * currentPrice;
                }

                if (previousValue > 0)
                {
                    var dailyReturn = (currentValue - previousValue) / previousValue;
                    returns.Add(dailyReturn);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating historical returns, using empty list");
        }

        return returns;
    }

    private async Task<double> GetStockPriceOnDate(int stockId, DateTime date)
    {
        var priceHistory = await _unitOfWork.StockPriceHistories.GetByStockAndDateAsync(stockId, date);
        if (priceHistory != null)
        {
            return (double)priceHistory.Close;
        }

        // Fallback to current price if historical data not available
        var stock = await _unitOfWork.Stocks.GetByIdAsync(stockId);
        return stock != null ? (double)stock.CurrentPrice : 0;
    }

    private double CalculateVolatility(List<double> returns)
    {
        if (returns.Count < 2) return 0;

        var mean = returns.Average();
        var variance = returns.Select(r => Math.Pow(r - mean, 2)).Average();
        return Math.Sqrt(variance * 252); // Annualized volatility
    }

    private double CalculateValueAtRisk(List<double> returns, double confidenceLevel)
    {
        if (returns.Count == 0) return 0;

        var sortedReturns = returns.OrderBy(r => r).ToList();
        var index = (int)Math.Ceiling((1 - confidenceLevel) * sortedReturns.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedReturns.Count - 1));
        
        return Math.Abs(sortedReturns[index]);
    }

    private double CalculateRiskScore(double volatility, double var95)
    {
        // Normalize risk score between 0 and 1
        var volatilityScore = Math.Min(volatility / 0.5, 1.0); // Assuming 50% as maximum volatility
        var varScore = Math.Min(var95 / 0.1, 1.0); // Assuming 10% as maximum daily VaR
        
        return (volatilityScore + varScore) / 2;
    }

    private string GetRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            < 0.3 => "Düşük Risk",
            < 0.6 => "Orta Risk",
            < 0.8 => "Yüksek Risk",
            _ => "Çok Yüksek Risk"
        };
    }

    private Task<double> CalculatePortfolioBeta(Core.Entities.Portfolio portfolio)
    {
        // Simplified beta calculation - in real implementation, you'd use market index data
        var weightedBeta = 0.0;
        var totalValue = portfolio.Items.Sum(item => item.Quantity * item.Stock.CurrentPrice);

        foreach (var item in portfolio.Items)
        {
            var weight = (double)(item.Quantity * item.Stock.CurrentPrice) / (double)totalValue;
            var stockBeta = EstimateStockBeta(item.Stock.Symbol); // Simplified estimation
            weightedBeta += weight * stockBeta;
        }

        return Task.FromResult(weightedBeta);
    }

    private double EstimateStockBeta(string symbol)
    {
        // Simplified beta estimation based on sector/stock characteristics
        // In a real implementation, you'd calculate beta using market correlation
        return symbol switch
        {
            "THYAO" or "PGSUS" => 1.2, // Airlines and travel - high beta
            "GARAN" or "AKBNK" or "YKBNK" => 1.1, // Banks - slightly high beta
            "BIMAS" or "EREGL" => 0.9, // Utilities and steel - lower beta
            "SISE" or "TUPRS" => 1.0, // Industrial - market beta
            _ => 1.0 // Default market beta
        };
    }

    private double CalculateSortinoRatio(List<double> returns, double riskFreeRate)
    {
        if (returns.Count == 0) return 0;

        var excessReturns = returns.Select(r => r - riskFreeRate / 252).ToList();
        var downsidenReturns = excessReturns.Where(r => r < 0).ToList();
        
        if (downsidenReturns.Count == 0) return double.PositiveInfinity;

        var downsideDeviation = Math.Sqrt(downsidenReturns.Select(r => r * r).Average()) * Math.Sqrt(252);
        var excessReturn = excessReturns.Average() * 252;

        return downsideDeviation > 0 ? excessReturn / downsideDeviation : 0;
    }

    private double CalculateMaximumDrawdown(List<double> returns)
    {
        if (returns.Count == 0) return 0;

        var cumulativeReturns = new List<double> { 1.0 };
        foreach (var ret in returns)
        {
            cumulativeReturns.Add(cumulativeReturns.Last() * (1 + ret));
        }

        var maxDrawdown = 0.0;
        var peak = cumulativeReturns[0];

        foreach (var value in cumulativeReturns)
        {
            if (value > peak)
            {
                peak = value;
            }
            else
            {
                var drawdown = (peak - value) / peak;
                maxDrawdown = Math.Max(maxDrawdown, drawdown);
            }
        }

        return maxDrawdown;
    }

    private double CalculateTrackingError(List<double> returns)
    {
        // Simplified tracking error - difference from market average
        if (returns.Count == 0) return 0;
        
        var marketReturn = 0.0003; // Assumed daily market return (rough estimate)
        var excessReturns = returns.Select(r => r - marketReturn).ToList();
        
        return CalculateVolatility(excessReturns);
    }

    private double CalculateDownsideDeviation(List<double> returns, double threshold)
    {
        if (returns.Count == 0) return 0;

        var downsideReturns = returns.Where(r => r < threshold).ToList();
        if (downsideReturns.Count == 0) return 0;

        var variance = downsideReturns.Select(r => Math.Pow(r - threshold, 2)).Average();
        return Math.Sqrt(variance * 252); // Annualized
    }

    private Dictionary<string, double> CalculateSectorAllocation(Core.Entities.Portfolio portfolio, decimal totalValue)
    {
        var sectorAllocation = new Dictionary<string, double>();
        
        foreach (var item in portfolio.Items)
        {
            var sector = GetStockSector(item.Stock.Symbol);
            var allocation = (double)(item.Quantity * item.Stock.CurrentPrice) / (double)totalValue;
            
            if (sectorAllocation.ContainsKey(sector))
                sectorAllocation[sector] += allocation;
            else
                sectorAllocation[sector] = allocation;
        }

        return sectorAllocation;
    }

    private string GetStockSector(string symbol)
    {
        // Simplified sector classification - in real implementation, use a proper sector database
        return symbol switch
        {
            "THYAO" or "PGSUS" => "Ulaştırma",
            "GARAN" or "AKBNK" or "YKBNK" or "VAKBN" => "Finansal Hizmetler",
            "BIMAS" or "EREGL" => "Sanayi",
            "SISE" or "TUPRS" => "Enerji",
            "ASELS" => "Teknoloji",
            "TTKOM" or "TCELL" => "Telekomünikasyon",
            "FROTO" or "ARCLK" => "Otomotiv",
            _ => "Diğer"
        };
    }

    private double CalculateConcentrationRisk(Core.Entities.Portfolio portfolio, decimal totalValue)
    {
        // Herfindahl-Hirschman Index
        var hhi = 0.0;
        
        foreach (var item in portfolio.Items)
        {
            var weight = (double)(item.Quantity * item.Stock.CurrentPrice) / (double)totalValue;
            hhi += weight * weight;
        }

        return hhi;
    }

    private double CalculateDiversificationScore(Dictionary<string, double> sectorAllocation, double concentrationRisk)
    {
        // Score based on sector diversification and concentration risk
        var sectorDiversification = 1.0 - concentrationRisk;
        var sectorCount = sectorAllocation.Count;
        var sectorScore = Math.Min(sectorCount / 8.0, 1.0); // Ideal: 8+ sectors
        
        return (sectorDiversification + sectorScore) / 2;
    }

    private double GetLargestHoldingPercentage(Core.Entities.Portfolio portfolio, decimal totalValue)
    {
        if (totalValue == 0) return 0;

        var largestHolding = portfolio.Items.Max(item => item.Quantity * item.Stock.CurrentPrice);
        return (double)(largestHolding / totalValue);
    }

    private double CalculateEffectiveNumberOfStocks(Core.Entities.Portfolio portfolio, decimal totalValue)
    {
        if (totalValue == 0) return 0;

        var sumOfSquaredWeights = 0.0;
        foreach (var item in portfolio.Items)
        {
            var weight = (double)(item.Quantity * item.Stock.CurrentPrice) / (double)totalValue;
            sumOfSquaredWeights += weight * weight;
        }

        return sumOfSquaredWeights > 0 ? 1.0 / sumOfSquaredWeights : 0;
    }

    private string GetRiskAssessmentText(double riskScore)
    {
        return riskScore switch
        {
            < 0.3 => "Düşük riskli, muhafazakar portföy",
            < 0.6 => "Orta riskli, dengeli portföy", 
            < 0.8 => "Yüksek riskli, büyüme odaklı portföy",
            _ => "Çok yüksek riskli, spekülatif portföy"
        };
    }

    private double CalculateOverallPortfolioScore(Dictionary<string, object> analysis)
    {
        var riskScore = (double)analysis["risk_score"];
        var diversificationScore = (double)analysis["diversification_score"];
        var sharpeRatio = (double)analysis["sharpe_ratio"];
        
        // Normalize Sharpe ratio (assume good is > 1.0)
        var normalizedSharpe = Math.Min(Math.Max(sharpeRatio, 0), 2.0) / 2.0;
        
        // Weight the scores
        var overallScore = (diversificationScore * 0.4) + ((1 - riskScore) * 0.3) + (normalizedSharpe * 0.3);
        
        return Math.Round(overallScore * 100, 1); // Convert to 0-100 scale
    }

    #endregion
} 