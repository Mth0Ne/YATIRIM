using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface IStockScraperService
{
    /// <summary>
    /// Mynet Finance'den tüm hisse senedi fiyatlarını çeker
    /// </summary>
    Task<IEnumerable<Stock>> GetCurrentStockPricesAsync();
    
    /// <summary>
    /// Belirtilen sembollerin fiyat verilerini çeker
    /// </summary>
    Task<IEnumerable<Stock>> GetCurrentStockPricesAsync(string[] symbols);
    
    /// <summary>
    /// Belirtilen hisse senedi için geçmiş fiyat verilerini çeker
    /// </summary>
    Task<IEnumerable<StockPriceHistory>> GetHistoricalDataAsync(string symbol);
} 