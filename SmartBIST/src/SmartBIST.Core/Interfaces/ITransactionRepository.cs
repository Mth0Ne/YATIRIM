using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetTransactionsByPortfolioIdAsync(int portfolioId);
    Task<IReadOnlyList<Transaction>> GetTransactionsByStockIdAsync(int stockId);
    Task<IReadOnlyList<Transaction>> GetTransactionsByPortfolioAndStockAsync(int portfolioId, int stockId);
    Task<IReadOnlyList<Transaction>> GetTransactionsByDateRangeAsync(int portfolioId, DateTime startDate, DateTime endDate);
} 