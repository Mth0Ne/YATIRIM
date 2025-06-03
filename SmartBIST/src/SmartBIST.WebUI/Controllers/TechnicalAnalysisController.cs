using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBIST.Application.Services;
using SmartBIST.Core.Interfaces;
using SmartBIST.WebUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace SmartBIST.WebUI.Controllers;
[Authorize]
public class TechnicalAnalysisController : Controller
{   
    private readonly ITechnicalIndicatorService _technicalIndicatorService;
    private readonly IStockService _stockService;
    private readonly IRealTechnicalAnalysisService _realTechnicalAnalysisService;
    private readonly ILogger<TechnicalAnalysisController> _logger;

    public TechnicalAnalysisController(        
        ITechnicalIndicatorService technicalIndicatorService,
        IStockService stockService,
        IRealTechnicalAnalysisService realTechnicalAnalysisService,
        ILogger<TechnicalAnalysisController> logger)    
    {        
        _technicalIndicatorService = technicalIndicatorService;
        _stockService = stockService;
        _realTechnicalAnalysisService = realTechnicalAnalysisService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? symbol = null, int period = 90)
    {
        var model = new TechnicalAnalysisViewModel();
        
        // Tüm hisse senetlerini yükle
        try
        {
            var allStocks = await _stockService.GetAllStocksAsync();
            model.AvailableStocks = allStocks.Select(s => new SelectListItem
            {
                Value = s.Symbol,
                Text = $"{s.Symbol} - {s.Name}"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available stocks");
            model.AvailableStocks = new List<SelectListItem>();
        }
        
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            try
            {
                // Gerçek API'den teknik analiz verilerini al
                var analysisResult = await _realTechnicalAnalysisService.GetTechnicalAnalysisAsync(symbol.Trim().ToUpper(), period);
                var priceHistoryResult = await _realTechnicalAnalysisService.GetPriceHistoryAsync(symbol.Trim().ToUpper(), period);
                
                model.Symbol = symbol.ToUpper();
                model.Period = period;
                model.IsDataLoaded = true;
                model.TechnicalAnalysis = analysisResult;
                model.PriceHistory = priceHistoryResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading technical analysis for symbol {Symbol}", symbol);
                model.ErrorMessage = $"Hisse senedi '{symbol}' için veri yüklenirken hata oluştu: {ex.Message}";
            }
        }
        
        return View(model);
    }

    // Clean Architecture: Real Technical Analysis Endpoints
    [HttpGet]
    public async Task<IActionResult> GetRealTechnicalAnalysis(string symbol, int period = 90)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            var result = await _realTechnicalAnalysisService.GetTechnicalAnalysisAsync(symbol.Trim().ToUpper(), period);
            
            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real technical analysis for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetRealPriceHistory(string symbol, int period = 90)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            var result = await _realTechnicalAnalysisService.GetPriceHistoryAsync(symbol.Trim().ToUpper(), period);
            
            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real price history for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Existing Mock/Database-based endpoints (backward compatibility)
    [HttpGet]
    public async Task<IActionResult> GetStockData(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            symbol = symbol.Trim().ToUpper();
            var stock = await _stockService.GetStockBySymbolAsync(symbol);
            
            if (stock == null)
            {
                return NotFound(new { error = $"Stock with symbol {symbol} not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    stockId = stock.Id,
                    symbol = stock.Symbol,
                    name = stock.Name,
                    currentPrice = stock.CurrentPrice,
                    dailyChange = stock.DailyChangePercentage,
                    volume = stock.Volume,
                    lastUpdated = stock.LastUpdated
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock data for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTechnicalAnalysis(int stockId, int period = 90)
    {
        try
        {
            var analysis = await _technicalIndicatorService.GetFullTechnicalAnalysisAsync(stockId, period);
            
            return Json(new
            {
                success = true,
                data = analysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technical analysis for stock {StockId}", stockId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIndicator(int stockId, string indicator, string period = "14", string fastPeriod = "12", string slowPeriod = "26", string signalPeriod = "9", string stdDev = "2.0")
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["period"] = period,
                ["fast"] = fastPeriod,
                ["slow"] = slowPeriod,
                ["signal"] = signalPeriod,
                ["k_period"] = period,
                ["d_period"] = "3",
                ["stddev"] = stdDev
            };

            var result = await _technicalIndicatorService.CalculateIndicatorAsync(stockId, indicator, parameters);
            
            return Json(new
            {
                success = !result.ContainsKey("error"),
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating indicator {Indicator} for stock {StockId}", indicator, stockId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPriceHistory(int stockId, int period = 90)
    {
        try
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-period);
            
            var priceHistory = await _stockService.GetStockPriceHistoryAsync(stockId, startDate, endDate);
            
            var chartData = priceHistory.Select(p => new
            {
                date = p.Date.ToString("yyyy-MM-dd"),
                open = p.Open,
                high = p.High,
                low = p.Low,
                close = p.Close,
                volume = p.Volume
            }).ToList();

            return Json(new
            {
                success = true,
                data = chartData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for stock {StockId}", stockId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> SearchStocks(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { success = true, data = new List<object>() });
            }

            var stocks = await _stockService.SearchStocksAsync(query);
            var results = stocks.Take(10).Select(s => new
            {
                id = s.Id,
                symbol = s.Symbol,
                name = s.Name,
                currentPrice = s.CurrentPrice,
                dailyChange = s.DailyChangePercentage
            }).ToList();

            return Json(new
            {
                success = true,
                data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching stocks with query {Query}", query);
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 