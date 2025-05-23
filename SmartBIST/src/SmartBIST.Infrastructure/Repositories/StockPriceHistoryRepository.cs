using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;
namespace SmartBIST.Infrastructure.Repositories;

public class StockPriceHistoryRepository : BaseRepository<StockPriceHistory>, IStockPriceHistoryRepository
{
    public StockPriceHistoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<StockPriceHistory>> GetPriceHistoryByStockIdAsync(int stockId)
    {
        return await _dbContext.StockPriceHistories
            .Where(sph => sph.StockId == stockId)
            .OrderBy(sph => sph.Date)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<StockPriceHistory>> GetPriceHistoryByDateRangeAsync(int stockId, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.StockPriceHistories
            .Where(sph => sph.StockId == stockId && sph.Date >= startDate && sph.Date <= endDate)
            .OrderBy(sph => sph.Date)
            .ToListAsync();
    }
    
    public async Task<StockPriceHistory?> GetLatestPriceAsync(int stockId)
    {
        return await _dbContext.StockPriceHistories
            .Where(sph => sph.StockId == stockId)
            .OrderByDescending(sph => sph.Date)
            .FirstOrDefaultAsync();
    }
    
    public async Task<StockPriceHistory?> GetByStockAndDateAsync(int stockId, DateTime date)
    {
        return await _dbContext.StockPriceHistories
            .Where(sph => sph.StockId == stockId && sph.Date.Date == date.Date)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IReadOnlyList<StockPriceHistory>> GetByStocksAndDateAsync(int[] stockIds, DateTime date)
    {
        return await _dbContext.StockPriceHistories
            .Where(sph => stockIds.Contains(sph.StockId) && sph.Date.Date == date.Date)
            .ToListAsync();
    }
} 