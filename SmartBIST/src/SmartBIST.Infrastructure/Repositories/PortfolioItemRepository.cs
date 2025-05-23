using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class PortfolioItemRepository : BaseRepository<PortfolioItem>, IPortfolioItemRepository
{
    public PortfolioItemRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<PortfolioItem>> GetItemsByPortfolioIdAsync(int portfolioId)
    {
        return await _dbContext.PortfolioItems
            .Include(pi => pi.Stock)
            .Where(pi => pi.PortfolioId == portfolioId)
            .ToListAsync();
    }
    
    public async Task<PortfolioItem?> GetItemWithStockAsync(int id)
    {
        return await _dbContext.PortfolioItems
            .Include(pi => pi.Stock)
            .FirstOrDefaultAsync(pi => pi.Id == id);
    }
    
    public async Task<PortfolioItem?> GetPortfolioItemByStockAsync(int portfolioId, int stockId)
    {
        return await _dbContext.PortfolioItems
            .Include(pi => pi.Stock)
            .FirstOrDefaultAsync(pi => pi.PortfolioId == portfolioId && pi.StockId == stockId);
    }
} 