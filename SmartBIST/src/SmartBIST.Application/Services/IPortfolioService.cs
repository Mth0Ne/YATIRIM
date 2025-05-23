using System.Collections.Generic;
using System.Threading.Tasks;
using SmartBIST.Application.DTOs;

namespace SmartBIST.Application.Services;

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioDto>> GetAllPortfoliosAsync();
    Task<IEnumerable<PortfolioDto>> GetUserPortfoliosAsync(string userId);
    Task<PortfolioDto> GetPortfolioByIdAsync(int id);
    Task<PortfolioWithStocksDto> GetPortfolioWithStocksAsync(int id);
    Task<int> CreatePortfolioAsync(PortfolioDto portfolioDto);
    Task UpdatePortfolioAsync(PortfolioDto portfolioDto);
    Task DeletePortfolioAsync(int id);
    
    // Portfolio Stock Operations
    Task<IEnumerable<PortfolioStockDto>> GetPortfolioStocksAsync(int portfolioId);
    Task<PortfolioStockDto?> GetPortfolioStockByIdAsync(int id);
    Task<int> CreatePortfolioStockAsync(PortfolioStockDto portfolioStockDto);
    Task UpdatePortfolioStockAsync(PortfolioStockDto portfolioStockDto);
    Task RemoveStockFromPortfolioAsync(int portfolioStockId);
    
    // Portfolio Analysis
    Task<decimal> GetPortfolioTotalValueAsync(int portfolioId);
    Task<decimal> GetPortfolioProfitLossAsync(int portfolioId);
    Task<decimal> GetPortfolioProfitLossPercentageAsync(int portfolioId);
    Task UpdatePortfolioPricesAsync(int portfolioId);
    
    // Portfolio Item Management
    Task<PortfolioItemDto> AddStockToPortfolioAsync(int portfolioId, int stockId, decimal quantity, decimal price, string userId);
    Task UpdatePortfolioItemAsync(int portfolioItemId, decimal quantity, decimal price, string userId);
    Task RemoveStockFromPortfolioAsync(int portfolioItemId, string userId);
    
    // Transaction Management
    Task<TransactionDto> AddTransactionAsync(TransactionDto transactionDto, string userId);
    Task<IEnumerable<TransactionDto>> GetPortfolioTransactionsAsync(int portfolioId, string userId);
    
    // Analysis
    Task<Dictionary<string, object>> GetPortfolioAnalysisAsync(int portfolioId, string userId);
    Task<Dictionary<string, object>> GetPortfolioRecommendationsAsync(int portfolioId, string userId);
} 