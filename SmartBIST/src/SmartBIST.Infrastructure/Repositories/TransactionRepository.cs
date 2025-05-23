using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<Transaction>> GetTransactionsByPortfolioIdAsync(int portfolioId)
    {
        return await _dbContext.Transactions
            .Include(t => t.Stock)
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<Transaction>> GetTransactionsByStockIdAsync(int stockId)
    {
        return await _dbContext.Transactions
            .Include(t => t.Portfolio)
            .Where(t => t.StockId == stockId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<Transaction>> GetTransactionsByPortfolioAndStockAsync(int portfolioId, int stockId)
    {
        return await _dbContext.Transactions
            .Where(t => t.PortfolioId == portfolioId && t.StockId == stockId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<Transaction>> GetTransactionsByDateRangeAsync(int portfolioId, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.Transactions
            .Include(t => t.Stock)
            .Where(t => t.PortfolioId == portfolioId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
} 