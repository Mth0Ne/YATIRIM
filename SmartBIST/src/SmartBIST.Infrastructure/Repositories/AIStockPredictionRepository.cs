using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class AIStockPredictionRepository : BaseRepository<AIStockPrediction>, IAIStockPredictionRepository
{
    public AIStockPredictionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByStockIdAsync(int stockId)
    {
        return await _dbContext.AIStockPredictions
            .Where(p => p.StockId == stockId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByUserIdAsync(string userId)
    {
        return await _dbContext.AIStockPredictions
            .Include(p => p.Stock)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByModelTypeAsync(int stockId, PredictionModel modelType)
    {
        return await _dbContext.AIStockPredictions
            .Where(p => p.StockId == stockId && p.ModelType == modelType)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
    
    public async Task<AIStockPrediction?> GetLatestPredictionAsync(int stockId, PredictionModel modelType)
    {
        return await _dbContext.AIStockPredictions
            .Where(p => p.StockId == stockId && p.ModelType == modelType)
            .OrderByDescending(p => p.CreatedDate)
            .FirstOrDefaultAsync();
    }
    
    public void Delete(AIStockPrediction entity)
    {
        _dbContext.AIStockPredictions.Remove(entity);
    }
} 