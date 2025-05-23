using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface IStockPriceHistoryRepository : IRepository<StockPriceHistory>
{
    Task<IReadOnlyList<StockPriceHistory>> GetPriceHistoryByStockIdAsync(int stockId);
    Task<IReadOnlyList<StockPriceHistory>> GetPriceHistoryByDateRangeAsync(int stockId, DateTime startDate, DateTime endDate);
    Task<StockPriceHistory?> GetLatestPriceAsync(int stockId);
    Task<StockPriceHistory?> GetByStockAndDateAsync(int stockId, DateTime date);
    Task<IReadOnlyList<StockPriceHistory>> GetByStocksAndDateAsync(int[] stockIds, DateTime date);
} 