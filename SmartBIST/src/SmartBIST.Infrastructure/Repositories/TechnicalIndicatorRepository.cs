using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class TechnicalIndicatorRepository : BaseRepository<TechnicalIndicator>, ITechnicalIndicatorRepository
{
    public TechnicalIndicatorRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByStockIdAsync(int stockId)
    {
        return await _dbContext.TechnicalIndicators
            .Where(ti => ti.StockId == stockId)
            .OrderByDescending(ti => ti.Date)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByTypeAsync(int stockId, IndicatorType indicatorType)
    {
        return await _dbContext.TechnicalIndicators
            .Where(ti => ti.StockId == stockId && ti.Type == indicatorType)
            .OrderByDescending(ti => ti.Date)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByDateRangeAsync(int stockId, IndicatorType indicatorType, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.TechnicalIndicators
            .Where(ti => ti.StockId == stockId && ti.Type == indicatorType && ti.Date >= startDate && ti.Date <= endDate)
            .OrderBy(ti => ti.Date)
            .ToListAsync();
    }
} 