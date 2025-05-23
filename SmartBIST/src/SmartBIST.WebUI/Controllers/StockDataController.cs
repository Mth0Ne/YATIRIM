using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBIST.WebUI.Controllers;

// Test amaçlı olarak herkesin erişebilmesi için Admin rolü şartını kaldırdık
//[Authorize(Roles = "Admin")]
public class StockDataController : Controller
{
    private readonly IStockScraperService _stockScraperService;
    private readonly IStockRepository _stockRepository;
    private readonly IStockPriceHistoryRepository _stockPriceHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StockDataController> _logger;

    public StockDataController(
        IStockScraperService stockScraperService,
        IStockRepository stockRepository,
        IStockPriceHistoryRepository stockPriceHistoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<StockDataController> logger)
    {
        _stockScraperService = stockScraperService;
        _stockRepository = stockRepository;
        _stockPriceHistoryRepository = stockPriceHistoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> FetchStockData()
    {
        try
        {
            _logger.LogInformation("Manuel hisse verisi çekme işlemi başlatıldı");
            
            // Hisse verilerini çek
            var stocks = await _stockScraperService.GetCurrentStockPricesAsync();
            
            // Boş sembolü olan hisseleri filtrele
            stocks = stocks.Where(s => !string.IsNullOrWhiteSpace(s.Symbol)).ToList();
            
            _logger.LogInformation($"Toplam {stocks.Count()} geçerli hisse verisi alındı (boş sembol olanlar filtrelendi)");
            
            if (!stocks.Any())
            {
                TempData["WarningMessage"] = "Çekilen hisse verilerinde geçerli sembol bulunamadı.";
                return RedirectToAction("Index");
            }
            
            var currentDate = DateTime.Now.Date;
            int newCount = 0;
            int updatedCount = 0;
            int errorCount = 0;
            
            // Her bir hisse için ayrı işlem yapalım
            foreach (var stockData in stocks)
            {
                try
                {
                    // Son bir kontrol daha yap
                    if (string.IsNullOrWhiteSpace(stockData.Symbol))
                    {
                        _logger.LogWarning($"Boş sembol değeri ile hisse atlanıyor");
                        errorCount++;
                        continue;
                    }
                        
                    // Veritabanında hissenin varlığını kontrol et
                    var existingStock = await _stockRepository.GetBySymbolAsync(stockData.Symbol);
                    
                    if (existingStock == null)
                    {
                        // Yeni hisseyi ekle
                        await _stockRepository.AddAsync(stockData);
                        await _unitOfWork.SaveChangesAsync();
                        
                        // Yeni hisse fiyat geçmişi ekle
                        var history = new StockPriceHistory
                        {
                            StockId = stockData.Id,
                            Date = currentDate,
                            Open = stockData.CurrentPrice,
                            High = stockData.CurrentPrice,
                            Low = stockData.CurrentPrice,
                            Close = stockData.CurrentPrice,
                            Volume = stockData.Volume > 0 ? stockData.Volume : 0
                        };
                        
                        await _stockPriceHistoryRepository.AddAsync(history);
                        await _unitOfWork.SaveChangesAsync();
                        
                        newCount++;
                    }
                    else
                    {
                        // Mevcut hisseyi güncelle
                        existingStock.CurrentPrice = stockData.CurrentPrice;
                        existingStock.DailyChangePercentage = stockData.DailyChangePercentage;
                        existingStock.Volume = stockData.Volume;
                        existingStock.LastUpdated = DateTime.Now;
                        
                        await _stockRepository.UpdateAsync(existingStock);
                        await _unitOfWork.SaveChangesAsync();
                        
                        // O günün verisini kontrol et, yoksa ekle
                        var existingHistory = await _stockPriceHistoryRepository.GetByStockAndDateAsync(existingStock.Id, currentDate);
                        
                        if (existingHistory == null)
                        {
                            var history = new StockPriceHistory
                            {
                                StockId = existingStock.Id,
                                Date = currentDate,
                                Open = stockData.CurrentPrice,
                                High = stockData.CurrentPrice,
                                Low = stockData.CurrentPrice,
                                Close = stockData.CurrentPrice,
                                Volume = stockData.Volume > 0 ? stockData.Volume : 0
                            };
                            
                            await _stockPriceHistoryRepository.AddAsync(history);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        else
                        {
                            // Günlük veriler birkaç kez güncellenmişse en yüksek ve en düşük değerleri takip et
                            existingHistory.High = Math.Max(existingHistory.High, stockData.CurrentPrice);
                            existingHistory.Low = Math.Min(existingHistory.Low, stockData.CurrentPrice);
                            existingHistory.Close = stockData.CurrentPrice;
                            existingHistory.Volume = stockData.Volume > 0 ? stockData.Volume : 0;
                            
                            await _stockPriceHistoryRepository.UpdateAsync(existingHistory);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Bir hisse için hata oluşursa, diğerlerine devam et
                    _logger.LogError(ex, $"Hisse {stockData.Symbol} işlenirken hata oluştu: {ex.Message}");
                    errorCount++;
                }
            }
            
            _logger.LogInformation($"Toplam {newCount} yeni, {updatedCount} güncellenen hisse senedi işlendi, {errorCount} hata oluştu");
            
            // Kullanıcıya başarılı mesajı göster
            if (errorCount > 0)
            {
                TempData["WarningMessage"] = $"Hisse verileri kısmen başarıyla çekildi. Toplam {newCount} yeni, {updatedCount} güncellenen hisse senedi işlendi. {errorCount} hisse işlenirken hata oluştu.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Hisse verileri başarıyla çekildi. Toplam {newCount} yeni, {updatedCount} güncellenen hisse senedi işlendi.";
            }
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hisse verilerini çekerken hata oluştu");
            
            // Kullanıcıya hata mesajı göster
            TempData["ErrorMessage"] = $"Hisse verileri çekilirken bir hata oluştu: {ex.Message}";
            
            return RedirectToAction("Index");
        }
    }
} 