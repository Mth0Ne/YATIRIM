using System;
using System.Threading.Tasks;

namespace SmartBIST.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IStockRepository Stocks { get; }
    IPortfolioRepository Portfolios { get; }
    IPortfolioItemRepository PortfolioItems { get; }
    ITransactionRepository Transactions { get; }
    IStockPriceHistoryRepository StockPriceHistories { get; }
    ITechnicalIndicatorRepository TechnicalIndicators { get; }
    IAIStockPredictionRepository AIStockPredictions { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
} 