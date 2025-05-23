using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartBIST.Infrastructure.Services;

public class StockDataUpdateService : BackgroundService
{
    private readonly ILogger<StockDataUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _dailyUpdateTime;
    
    public StockDataUpdateService(
        ILogger<StockDataUpdateService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Günlük güncelleme saati (17:30)
        _dailyUpdateTime = new TimeSpan(17, 30, 0);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Data Update Service başlatıldı");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Bir sonraki çalışma zamanını hesapla
                var nextRunTime = CalculateNextRunTime();
                var delay = nextRunTime - DateTime.Now;
                
                _logger.LogInformation($"Bir sonraki veri güncellemesi {nextRunTime:yyyy-MM-dd HH:mm:ss}'de gerçekleşecek (yaklaşık {delay.TotalHours:F1} saat sonra)");
                
                // Bir sonraki çalışma zamanına kadar bekle
                await Task.Delay(delay, stoppingToken);
                
                // Güncelleme işlemini çalıştır
                await UpdateStockDataAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, "Stock Data Update Service'de hata oluştu");
                
                // Hata durumunda 15 dakika bekle ve tekrar dene
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
    
    private DateTime CalculateNextRunTime()
    {
        var now = DateTime.Now;
        var today = now.Date.Add(_dailyUpdateTime);
        
        // Eğer belirlenen saat geçtiyse, sonraki gün aynı saate ayarla
        if (now >= today)
        {
            return today.AddDays(1);
        }
        
        return today;
    }
    
    private async Task UpdateStockDataAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hisse senedi verilerini güncelleme işlemi başlatıldı");
        
        try
        {
            // DbContext'in doğru şekilde kullanılması için her seferinde yeni bir scope oluşturuyoruz
            using var scope = _serviceProvider.CreateScope();
            var stockScraper = scope.ServiceProvider.GetRequiredService<IStockScraperService>();
            var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IStockPriceHistoryRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            // Tüm hisse senetlerinin verilerini çek
            var stocks = await stockScraper.GetCurrentStockPricesAsync();
            var currentDate = DateTime.Now.Date;
            int updatedCount = 0;
            
            foreach (var stockData in stocks)
            {
                // Veritabanında hissenin varlığını kontrol et
                var existingStock = await stockRepository.GetBySymbolAsync(stockData.Symbol);
                
                if (existingStock == null)
                {
                    // Yeni hisse kaydet
                    await stockRepository.AddAsync(stockData);
                    
                    // Geçmiş veri kaydet
                    var history = new StockPriceHistory
                    {
                        StockId = stockData.Id, // ID repository.Add tarafından atanmış olacak
                        Date = currentDate,
                        Open = stockData.CurrentPrice,
                        High = stockData.CurrentPrice,
                        Low = stockData.CurrentPrice,
                        Close = stockData.CurrentPrice,
                        Volume = stockData.Volume > 0 ? stockData.Volume : 0
                    };
                    
                    await historyRepository.AddAsync(history);
                }
                else
                {
                    // Mevcut hisseyi güncelle
                    existingStock.CurrentPrice = stockData.CurrentPrice;
                    existingStock.DailyChangePercentage = stockData.DailyChangePercentage;
                    existingStock.Volume = stockData.Volume;
                    existingStock.LastUpdated = DateTime.Now;
                    
                    await stockRepository.UpdateAsync(existingStock);
                    
                    // O günün verisini kontrol et, yoksa ekle
                    var existingHistory = await historyRepository.GetByStockAndDateAsync(existingStock.Id, currentDate);
                    
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
                        
                        await historyRepository.AddAsync(history);
                    }
                    else
                    {
                        // Günlük veriler birkaç kez güncellenmişse en yüksek ve en düşük değerleri takip et
                        existingHistory.High = Math.Max(existingHistory.High, stockData.CurrentPrice);
                        existingHistory.Low = Math.Min(existingHistory.Low, stockData.CurrentPrice);
                        existingHistory.Close = stockData.CurrentPrice;
                        existingHistory.Volume = stockData.Volume > 0 ? stockData.Volume : 0;
                        
                        await historyRepository.UpdateAsync(existingHistory);
                    }
                }
                
                updatedCount++;
                
                // Her 10 işlemde bir değişiklikleri kaydet
                if (updatedCount % 10 == 0)
                {
                    await unitOfWork.SaveChangesAsync();
                }
            }
            
            // Kalan değişiklikleri kaydet
            await unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation($"Toplam {updatedCount} hisse senedinin verisi güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hisse senedi verilerini güncelleme sırasında hata oluştu");
            throw;
        }
    }
} 