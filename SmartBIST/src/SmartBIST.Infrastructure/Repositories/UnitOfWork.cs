using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace SmartBIST.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private IDbContextTransaction? _transaction;
    private bool _disposed;
    
    // Repository instances
    private IPortfolioRepository? _portfolioRepository;
    private IStockRepository? _stockRepository;
    private IPortfolioItemRepository? _portfolioItemRepository;
    private ITransactionRepository? _transactionRepository;
    private IStockPriceHistoryRepository? _stockPriceHistoryRepository;
    private ITechnicalIndicatorRepository? _technicalIndicatorRepository;
    private IAIStockPredictionRepository? _aiStockPredictionRepository;
    
    public UnitOfWork(
        ApplicationDbContext dbContext, 
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
        
        _portfolioRepository = new PortfolioRepository(_dbContext);
        _stockRepository = new StockRepository(_dbContext, _cache);
        _portfolioItemRepository = new PortfolioItemRepository(_dbContext);
        _transactionRepository = new TransactionRepository(_dbContext);
        _stockPriceHistoryRepository = new StockPriceHistoryRepository(_dbContext);
        _technicalIndicatorRepository = new TechnicalIndicatorRepository(_dbContext);
        _aiStockPredictionRepository = new AIStockPredictionRepository(_dbContext);
    }
    
    public IPortfolioRepository Portfolios => _portfolioRepository ??= new PortfolioRepository(_dbContext);
    public IStockRepository Stocks => _stockRepository ??= new StockRepository(_dbContext, _cache);
    public IPortfolioItemRepository PortfolioItems => _portfolioItemRepository ??= new PortfolioItemRepository(_dbContext);
    public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_dbContext);
    public IStockPriceHistoryRepository StockPriceHistories => _stockPriceHistoryRepository ??= new StockPriceHistoryRepository(_dbContext);
    public ITechnicalIndicatorRepository TechnicalIndicators => _technicalIndicatorRepository ??= new TechnicalIndicatorRepository(_dbContext);
    public IAIStockPredictionRepository AIStockPredictions => _aiStockPredictionRepository ??= new AIStockPredictionRepository(_dbContext);
    
    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
    
    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _dbContext.Dispose();
            _transaction?.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
} 