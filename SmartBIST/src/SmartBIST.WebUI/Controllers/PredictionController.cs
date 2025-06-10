using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBIST.Application.DTOs;
using SmartBIST.Application.Services;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.WebUI.Models;

namespace SmartBIST.WebUI.Controllers;

[Authorize]
public class PredictionController : Controller
{
    private readonly IPredictionService _predictionService;
    private readonly IStockService _stockService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PredictionController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    
    public PredictionController(
        IPredictionService predictionService,
        IStockService stockService,
        UserManager<ApplicationUser> userManager,
        ILogger<PredictionController> logger,
        IUnitOfWork unitOfWork)
    {
        _predictionService = predictionService;
        _stockService = stockService;
        _userManager = userManager;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var predictionDtos = await _predictionService.GetUserPredictionsAsync(userId!);
        
        var predictions = predictionDtos.Select(p => new Models.PredictionListItemViewModel
        {
            Id = p.Id,
            StockId = p.StockId,
            StockSymbol = p.StockSymbol,
            StockName = p.StockName,
            UserId = p.UserId,
            ModelType = p.ModelType,
            CreatedDate = p.CreatedDate,
            StartDate = p.PredictionStartDate,
            EndDate = p.PredictionEndDate,
            Parameters = p.Parameters?.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()),
            Accuracy = p.Accuracy
        }).ToList();
        
        return View(predictions);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var prediction = await _predictionService.GetPredictionByIdAsync(id, _userManager.GetUserId(User)!);
        
        if (prediction == null)
        {
            return NotFound();
        }
        
        var stock = await _stockService.GetStockByIdAsync(prediction.StockId);
        
        var viewModel = new PredictionDetailViewModel
        {
            Id = prediction.Id,
            StockSymbol = stock?.Symbol ?? "Bilinmiyor",
            StockName = stock?.Name ?? "Bilinmiyor",
            ModelType = prediction.ModelType,
            CreatedDate = prediction.CreatedDate,
            StartDate = prediction.PredictionStartDate,
            EndDate = prediction.PredictionEndDate,
            TrainingWindow = prediction.Parameters != null && prediction.Parameters.TryGetValue("training_window", out var tw) ? int.Parse(tw) : 60,
            ConfidenceLevel = prediction.Parameters != null && prediction.Parameters.TryGetValue("confidence_level", out var cl) ? double.Parse(cl) : 0.95,
            IncludeTechnicalIndicators = prediction.Parameters != null && prediction.Parameters.TryGetValue("include_technical_indicators", out var ti) ? bool.Parse(ti) : true,
            IncludeSentimentAnalysis = prediction.Parameters != null && prediction.Parameters.TryGetValue("include_sentiment_analysis", out var sa) ? bool.Parse(sa) : false,
            
            // API'den gelen değerleri direkt aktaralım
            PredictedPrice = prediction.PredictedPrice > 0 ? prediction.PredictedPrice : 0,
            CurrentPrice = prediction.CurrentPrice > 0 ? prediction.CurrentPrice : (stock?.CurrentPrice ?? 0),
            PriceChange = prediction.PriceChange,
            PercentChange = prediction.PercentChange,
            DataPoints = prediction.DataPoints,
            PredictionDate = prediction.PredictionDate,
            LastCloseDate = prediction.LastCloseDate,
            
            // Başarı durumu kontrol ediliyor
            IsSuccess = prediction.Success,
            ErrorMessage = prediction.ErrorMessage,
            
            // Performance metrics - performans metrikleri (API'den gelen gerçek değerler)
            Accuracy = prediction.Accuracy,
            MeanAbsoluteError = prediction.PredictionData != null && prediction.PredictionData.TryGetValue("mae", out var mae) ? SafeToDecimal(mae) : 0,
            RootMeanSquaredError = prediction.PredictionData != null && prediction.PredictionData.TryGetValue("rmse", out var rmse) ? SafeToDecimal(rmse) : 0,
            RSquared = prediction.PredictionData != null && prediction.PredictionData.TryGetValue("r2", out var r2) ? SafeToDecimal(r2) : 0
        };
        
        // API'den gelen değerlerin gözden geçirilmesi
        if (!prediction.Success || prediction.PredictedPrice <= 0)
        {
            _logger.LogWarning("Geçersiz tahmin verileri. PredictedPrice={0}, Success={1}, ErrorMessage={2}", 
                prediction.PredictedPrice, prediction.Success, prediction.ErrorMessage);
            viewModel.IsPredictionDataMissing = true;
        }
        else
        {
            _logger.LogInformation("API verileri doğru alındı: Symbol={0}, PredictedPrice={1}, CurrentPrice={2}, PriceChange={3}, PercentChange={4}", 
                prediction.StockSymbol, prediction.PredictedPrice, prediction.CurrentPrice, prediction.PriceChange, prediction.PercentChange);
        }
        
        // Grafik verileri hazırlama
        PrepareChartData(viewModel, prediction);
        
        return View(viewModel);
    }
    
    private void PrepareChartData(PredictionDetailViewModel viewModel, PredictionResultDto prediction)
    {
        // Grafik verileri hazırlama
        if (prediction.PredictionData != null)
        {
            _logger.LogInformation($"Prediction data: {System.Text.Json.JsonSerializer.Serialize(prediction.PredictionData)}");
            
            // Grafik verilerini ekle
            if (prediction.PredictionData.TryGetValue("actual_values", out var actual) && 
                prediction.PredictionData.TryGetValue("predicted_values", out var predicted))
            {
                try
                {
                    _logger.LogInformation($"Veri tipleri - actual: {actual?.GetType().FullName}, predicted: {predicted?.GetType().FullName}");
                    
                    // Her durumda yeni temiz listeler oluşturalım
                    viewModel.ActualPrices.Clear();
                    viewModel.PredictedPrices.Clear();
                    viewModel.DateLabels.Clear();
                    
                    // Mevcut fiyat ve tahmin edilen fiyat - bunları kesin biliyoruz
                    decimal currentPriceValue = viewModel.CurrentPrice;
                    decimal predictedPriceValue = viewModel.PredictedPrice;
                    
                    // Değerleri logla
                    _logger.LogInformation($"CurrentPrice from viewModel: {viewModel.CurrentPrice}, PredictedPrice: {viewModel.PredictedPrice}");
                    
                    // Değerlerin doğruluğunu kontrol et - eğer 0 ise stock'tan tekrar al
                    if (currentPriceValue <= 0 && prediction.StockId > 0)
                    {
                        var stock = _stockService.GetStockByIdAsync(prediction.StockId).Result;
                        if (stock != null && stock.CurrentPrice > 0)
                        {
                            currentPriceValue = stock.CurrentPrice;
                            _logger.LogInformation($"CurrentPrice was 0, updated from stock data: {currentPriceValue}");
                        }
                    }
                    
                    // Bugünün tarihi ve tahmin tarihi - bunlar da kesin
                    viewModel.DateLabels.Add(DateTime.Today.ToString("MM/dd"));
                    viewModel.DateLabels.Add(viewModel.EndDate.ToString("MM/dd"));
                    
                    // Gerçek fiyat - sadece bugünkü değeri biliyoruz
                    viewModel.ActualPrices.Add(currentPriceValue);
                    
                    // Tahmin verileri - bugünden tahmine
                    viewModel.PredictedPrices.Add(currentPriceValue);
                    viewModel.PredictedPrices.Add(predictedPriceValue);
                    
                    _logger.LogInformation($"Chart data loaded: Current={currentPriceValue}, Predicted={predictedPriceValue}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing prediction data for chart");
                    CreateFallbackChartData(viewModel, prediction.StockId);
                }
            }
            else
            {
                CreateFallbackChartData(viewModel, prediction.StockId);
            }
        }
        else
        {
            CreateFallbackChartData(viewModel, prediction.StockId);
        }
    }
    
    private void CreateFallbackChartData(PredictionDetailViewModel viewModel, int stockId)
    {
        // Grafik için yedek veriler oluştur
        var stock = _stockService.GetStockByIdAsync(stockId).Result;
        decimal currentPriceValue = viewModel.CurrentPrice;
        
        // Eğer mevcut fiyat 0 ise, stock'tan al
        if (currentPriceValue <= 0 && stock?.CurrentPrice > 0)
        {
            currentPriceValue = stock.CurrentPrice;
            _logger.LogInformation($"In fallback chart: CurrentPrice was 0, updated from stock data: {currentPriceValue}");
        }
        else if (currentPriceValue <= 0)
        {
            // Yedek olarak makul bir varsayılan değer kullan
            currentPriceValue = 100m;
            _logger.LogWarning($"In fallback chart: CurrentPrice is still 0, using default value: {currentPriceValue}");
        }
        
        // Önce listeleri temizleyelim - mükerrer veri olmasın
        viewModel.ActualPrices.Clear();
        viewModel.PredictedPrices.Clear();
        viewModel.DateLabels.Clear();
        
        // Sadece iki veri noktası oluşturalım - bugün ve tahmin tarihi
        // 1. Bugünün tarihi ve mevcut fiyat
        viewModel.DateLabels.Add(DateTime.Today.ToString("MM/dd"));
        viewModel.ActualPrices.Add(currentPriceValue);
        
        // 2. Tahmin tarihi ve tahmin edilen fiyat
        viewModel.DateLabels.Add(viewModel.EndDate.ToString("MM/dd"));
        
        // Tahmin verisi: 
        // İlk noktada (bugün) "şu anki fiyat"
        viewModel.PredictedPrices.Add(currentPriceValue);
        
        // İkinci noktada (tahmin tarihi) "tahmin edilen fiyat"
        decimal predictedPriceValue = viewModel.PredictedPrice > 0 ? viewModel.PredictedPrice : currentPriceValue * 1.05m;
        viewModel.PredictedPrices.Add(predictedPriceValue);
        
        _logger.LogInformation($"Fallback grafik verisi oluşturuldu: Bugün={currentPriceValue}, Tahmin={predictedPriceValue}");
    }
    
    public async Task<IActionResult> Create()
    {
        try 
        {
            var stocks = await _stockService.GetAllStocksAsync();
            
            if (stocks == null || !stocks.Any())
            {
                _logger.LogWarning("Hisse listesi boş veya null geldi");
                ViewBag.NoStocks = "Sistemde kayıtlı hisse senedi bulunamadı.";
            }
            
            _logger.LogInformation($"Hisse listesi: {stocks?.Count() ?? 0} adet hisse bulundu");
            
            ViewBag.Stocks = new SelectList(stocks ?? Enumerable.Empty<StockDto>(), "Id", "Symbol");
            
            // Sadece LSTM modeli kullanacağız şimdilik
            ViewBag.Models = new SelectList(
                new[] { new { Id = 0, Name = "LSTM" } }, 
                "Id", "Name");
                
            var viewModel = new CreatePredictionViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                ModelType = 0 // LSTM
            };
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin oluşturma sayfası yüklenirken hata oluştu");
            ViewBag.ErrorMessage = "Hisse listesi yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            return View(new CreatePredictionViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                ModelType = 0 // LSTM
            });
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePredictionViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var stocks = await _stockService.GetAllStocksAsync();
            
            ViewBag.Stocks = new SelectList(stocks, "Id", "Symbol");
            
            // Sadece LSTM modeli kullanacağız şimdilik
            ViewBag.Models = new SelectList(
                new[] { new { Id = 0, Name = "LSTM" } }, 
                "Id", "Name");
                
            return View(viewModel);
        }
        
        try
        {
            var userId = _userManager.GetUserId(User);
            
            var requestDto = new PredictionRequestDto
            {
                StockId = viewModel.StockId,
                StartDate = viewModel.StartDate,
                EndDate = viewModel.EndDate,
                ModelType = PredictionModel.LSTM, // Sadece LSTM
                Parameters = viewModel.GetParametersDictionary()
            };
            
            var prediction = await _predictionService.GetPricePredictionAsync(requestDto, userId!);
            
            return RedirectToAction(nameof(Details), new { id = prediction.Id });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to prediction API");
            ModelState.AddModelError("", "Tahmin API'sine bağlanırken bir hata oluştu. Lütfen API'nin çalıştığından emin olun.");
            
            var stocks = await _stockService.GetAllStocksAsync();
            
            ViewBag.Stocks = new SelectList(stocks, "Id", "Symbol");
            
            // Sadece LSTM modeli kullanacağız şimdilik
            ViewBag.Models = new SelectList(
                new[] { new { Id = 0, Name = "LSTM" } }, 
                "Id", "Name");
                
            ViewBag.ApiError = true;
            ViewBag.ApiErrorMessage = "Python tahmin API'sine bağlanılamadı. API'nin çalıştığından emin olun (http://localhost:5000).";
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prediction");
            ModelState.AddModelError("", "An error occurred while creating the prediction. Please try again.");
            
            var stocks = await _stockService.GetAllStocksAsync();
            
            ViewBag.Stocks = new SelectList(stocks, "Id", "Symbol");
            
            // Sadece LSTM modeli kullanacağız şimdilik
            ViewBag.Models = new SelectList(
                new[] { new { Id = 0, Name = "LSTM" } }, 
                "Id", "Name");
                
            return View(viewModel);
        }
    }
    
    public async Task<IActionResult> MarketInsights()
    {
        var insights = await _predictionService.GetMarketInsightsAsync();
        return View(insights);
    }
    
    // Silme işlemleri için GET ve POST metotları
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var prediction = await _predictionService.GetPredictionByIdAsync(id, userId);
        
        if (prediction == null)
        {
            return NotFound();
        }
        
        return View(prediction);
    }
    
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Tahmin silme işlemi
            var result = await _unitOfWork.AIStockPredictions.GetByIdAsync(id);
            
            if (result == null || result.UserId != userId)
            {
                return NotFound();
            }
            
            _unitOfWork.AIStockPredictions.Delete(result);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tahmin başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin silinirken hata oluştu. ID: {ID}", id);
            TempData["ErrorMessage"] = "Tahmin silinirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }
    
    private static decimal SafeToDecimal(object value)
    {
        try
        {
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return (decimal)jsonElement.GetDouble();
                }
                return 0;
            }
            
            if (value is double doubleValue)
            {
                return (decimal)doubleValue;
            }
            
            if (value is decimal decimalValue)
            {
                return decimalValue;
            }
            
            if (value is int intValue)
            {
                return intValue;
            }
            
            if (decimal.TryParse(value?.ToString(), out var parsed))
            {
                return parsed;
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }
} 