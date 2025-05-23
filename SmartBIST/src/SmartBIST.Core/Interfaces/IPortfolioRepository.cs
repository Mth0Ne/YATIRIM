using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<IReadOnlyList<Portfolio>> GetUserPortfoliosAsync(string userId);
    Task<Portfolio?> GetPortfolioWithItemsAsync(int id);
    Task<Portfolio?> GetPortfolioWithItemsAndStocksAsync(int id);
    Task<Portfolio?> GetPortfolioWithItemsAndTransactionsAsync(int id);
    Task<Portfolio?> GetCompletePortfolioAsync(int id);
} 