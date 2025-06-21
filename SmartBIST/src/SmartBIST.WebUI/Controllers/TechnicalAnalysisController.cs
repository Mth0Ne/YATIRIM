using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBIST.Application.Services;
using SmartBIST.Application.DTOs;
using SmartBIST.Core.Interfaces;
using SmartBIST.WebUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace SmartBIST.WebUI.Controllers;
// [Authorize] // Geçici olarak kaldırıldı - test için
public class TechnicalAnalysisController : Controller
{   
    private readonly IStockService _stockService;
    private readonly IRealTechnicalAnalysisService _realTechnicalAnalysisService;
    private readonly IDataMiningAnalysisService _dataMiningAnalysisService;
    private readonly ILogger<TechnicalAnalysisController> _logger;

    public TechnicalAnalysisController(        
        IStockService stockService,
        IRealTechnicalAnalysisService realTechnicalAnalysisService,
        IDataMiningAnalysisService dataMiningAnalysisService,
        ILogger<TechnicalAnalysisController> logger)    
    {        
        _stockService = stockService;
        _realTechnicalAnalysisService = realTechnicalAnalysisService;
        _dataMiningAnalysisService = dataMiningAnalysisService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? symbol = null, int period = 90, bool enableDataMining = false, bool includePredictions = true)
    {
        var model = new TechnicalAnalysisViewModel
        {
            EnableDataMining = enableDataMining,
            IncludePredictions = includePredictions
        };
        
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
                model.Symbol = symbol.ToUpper();
                model.Period = period;
                model.IsDataLoaded = true;

                if (enableDataMining)
                {
                    // Veri madenciliği analizi
                    var dataMiningResult = await _dataMiningAnalysisService.GetDataMiningAnalysisAsync(symbol.Trim().ToUpper(), period, includePredictions);
                    model.DataMiningAnalysis = dataMiningResult;
                    
                    // Price history'yi data mining analysis'den al
                    model.PriceHistory = new PriceHistoryResultDto
                    {
                        Symbol = dataMiningResult.Symbol,
                        DataPoints = dataMiningResult.DataPoints,
                        PriceHistory = dataMiningResult.PriceHistory
                    };
                }
                else
                {
                    // Klasik teknik analiz
                    var analysisResult = await _realTechnicalAnalysisService.GetTechnicalAnalysisAsync(symbol.Trim().ToUpper(), period);
                    model.TechnicalAnalysis = analysisResult;
                    
                    // Price history'yi technical analysis'den al
                    model.PriceHistory = new PriceHistoryResultDto
                    {
                        Symbol = analysisResult.Symbol,
                        DataPoints = analysisResult.DataPoints,
                        PriceHistory = analysisResult.PriceHistory
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analysis for symbol {Symbol}", symbol);
                model.ErrorMessage = $"Hisse senedi '{symbol}' için veri yüklenirken hata oluştu: {ex.Message}";
            }
        }
        
        return View(model);
    }

    // Data Mining Analysis Endpoints
    [HttpGet]
    public async Task<IActionResult> GetDataMiningAnalysis(string symbol, int period = 90, bool predictions = true)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            if (symbol.Length > 10)
            {
                return BadRequest(new { error = "Symbol must be 10 characters or less" });
            }

            if (period < 5 || period > 1000)
            {
                return BadRequest(new { error = "Period must be between 5 and 1000 days" });
            }

            symbol = symbol.Trim().ToUpper();

            var result = await _dataMiningAnalysisService.GetDataMiningAnalysisAsync(symbol, period, predictions);
            
            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error for symbol {Symbol}", symbol);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data mining analysis for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Clean Architecture: Real Technical Analysis Endpoints
    [HttpGet]
    public async Task<IActionResult> GetRealTechnicalAnalysis(string symbol, int period = 90)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            if (symbol.Length > 10)
            {
                return BadRequest(new { error = "Symbol must be 10 characters or less" });
            }

            if (period < 5 || period > 1000)
            {
                return BadRequest(new { error = "Period must be between 5 and 1000 days" });
            }

            symbol = symbol.Trim().ToUpper();

            var result = await _realTechnicalAnalysisService.GetTechnicalAnalysisAsync(symbol, period);
            
            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error for symbol {Symbol}", symbol);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real technical analysis for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetRealPriceHistory(string symbol, int period = 90)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { error = "Stock symbol is required" });
            }

            if (symbol.Length > 10)
            {
                return BadRequest(new { error = "Symbol must be 10 characters or less" });
            }

            if (period < 5 || period > 1000)
            {
                return BadRequest(new { error = "Period must be between 5 and 1000 days" });
            }

            symbol = symbol.Trim().ToUpper();

            var result = await _realTechnicalAnalysisService.GetPriceHistoryAsync(symbol, period);
            
            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error for symbol {Symbol}", symbol);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real price history for symbol {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
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