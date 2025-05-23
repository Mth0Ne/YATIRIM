using SmartBIST.Core.Entities;

namespace SmartBIST.Core.Interfaces;

public interface ITechnicalIndicatorRepository : IRepository<TechnicalIndicator>
{
    Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByStockIdAsync(int stockId);
    Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByTypeAsync(int stockId, IndicatorType indicatorType);
    Task<IReadOnlyList<TechnicalIndicator>> GetIndicatorsByDateRangeAsync(int stockId, IndicatorType indicatorType, DateTime startDate, DateTime endDate);
} 