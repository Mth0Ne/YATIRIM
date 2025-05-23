using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface IPortfolioItemRepository : IRepository<PortfolioItem>
{
    Task<IReadOnlyList<PortfolioItem>> GetItemsByPortfolioIdAsync(int portfolioId);
    Task<PortfolioItem?> GetItemWithStockAsync(int id);
    Task<PortfolioItem?> GetPortfolioItemByStockAsync(int portfolioId, int stockId);
} 