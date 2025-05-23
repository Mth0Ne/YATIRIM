using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface IAIStockPredictionRepository : IRepository<AIStockPrediction>
{
    Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByStockIdAsync(int stockId);
    Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByUserIdAsync(string userId);
    Task<IReadOnlyList<AIStockPrediction>> GetPredictionsByModelTypeAsync(int stockId, PredictionModel modelType);
    Task<AIStockPrediction?> GetLatestPredictionAsync(int stockId, PredictionModel modelType);
    void Delete(AIStockPrediction entity);
} 