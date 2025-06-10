using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;

namespace SmartBIST.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PortfolioService> _logger;
    
    public PortfolioService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PortfolioService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<IEnumerable<PortfolioDto>> GetAllPortfoliosAsync()
    {
        var portfolios = await _unitOfWork.Portfolios.GetAllAsync();
        return _mapper.Map<IEnumerable<PortfolioDto>>(portfolios);
    }
    
    public async Task<IEnumerable<PortfolioDto>> GetUserPortfoliosAsync(string userId)
    {
        var portfolios = await _unitOfWork.Portfolios.GetUserPortfoliosAsync(userId);
        return _mapper.Map<IEnumerable<PortfolioDto>>(portfolios);
    }
    
    public async Task<PortfolioDto> GetPortfolioByIdAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(id);
        if (portfolio == null)
            return new PortfolioDto();
            
        return _mapper.Map<PortfolioDto>(portfolio);
    }
    
    public async Task<int> CreatePortfolioAsync(PortfolioDto portfolioDto)
    {
        var portfolio = _mapper.Map<Portfolio>(portfolioDto);
        await _unitOfWork.Portfolios.AddAsync(portfolio);
        await _unitOfWork.SaveChangesAsync();
        
        return portfolio.Id;
    }
    
    public async Task UpdatePortfolioAsync(PortfolioDto portfolioDto)
    {
        var existingPortfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioDto.Id);
        
        if (existingPortfolio == null)
        {
            throw new ArgumentException($"Portfolio with ID {portfolioDto.Id} not found");
        }
        
        existingPortfolio.Name = portfolioDto.Name;
        existingPortfolio.Description = portfolioDto.Description;
        existingPortfolio.UpdatedDate = DateTime.UtcNow;
        
        await _unitOfWork.Portfolios.UpdateAsync(existingPortfolio);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task DeletePortfolioAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(id);
        
        if (portfolio == null)
        {
            throw new ArgumentException($"Portfolio with ID {id} not found");
        }
        
        await _unitOfWork.Portfolios.DeleteAsync(portfolio);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<PortfolioStockDto>> GetPortfolioStocksAsync(int portfolioId)
    {
        var portfolioItems = await _unitOfWork.PortfolioItems.GetItemsByPortfolioIdAsync(portfolioId);
        var result = new List<PortfolioStockDto>();
        
        foreach(var item in portfolioItems)
        {
            var stock = await _unitOfWork.Stocks.GetByIdAsync(item.StockId);
            if (stock != null)
            {
                result.Add(new PortfolioStockDto
                {
                    Id = item.Id,
                    PortfolioId = item.PortfolioId,
                    StockId = item.StockId,
                    StockSymbol = stock.Symbol,
                    StockName = stock.Name,
                    Quantity = item.Quantity,
                    PurchasePrice = item.AveragePrice,
                    CurrentPrice = stock.CurrentPrice,
                    PurchaseDate = item.CreatedDate
                });
            }
        }
        
        return result;
    }
    
    public async Task<PortfolioStockDto?> GetPortfolioStockByIdAsync(int id)
    {
        var item = await _unitOfWork.PortfolioItems.GetByIdAsync(id);
        if (item == null)
            return null;
            
        var stock = await _unitOfWork.Stocks.GetByIdAsync(item.StockId);
        if (stock == null)
            return null;
            
        return new PortfolioStockDto
        {
            Id = item.Id,
            PortfolioId = item.PortfolioId,
            StockId = item.StockId,
            StockSymbol = stock.Symbol,
            StockName = stock.Name,
            Quantity = item.Quantity,
            PurchasePrice = item.AveragePrice,
            CurrentPrice = stock.CurrentPrice,
            PurchaseDate = item.CreatedDate
        };
    }
    
    public async Task<int> CreatePortfolioStockAsync(PortfolioStockDto portfolioStockDto)
    {
        var portfolioItem = new PortfolioItem
        {
            PortfolioId = portfolioStockDto.PortfolioId,
            StockId = portfolioStockDto.StockId,
            Quantity = portfolioStockDto.Quantity,
            AveragePrice = portfolioStockDto.PurchasePrice,
            CreatedDate = DateTime.UtcNow
        };
        
        await _unitOfWork.PortfolioItems.AddAsync(portfolioItem);
        await _unitOfWork.SaveChangesAsync();
        
        return portfolioItem.Id;
    }
    
    public async Task UpdatePortfolioStockAsync(PortfolioStockDto portfolioStockDto)
    {
        var item = await _unitOfWork.PortfolioItems.GetByIdAsync(portfolioStockDto.Id);
        if (item == null)
            throw new ArgumentException($"Portfolio stock with ID {portfolioStockDto.Id} not found");
            
        item.Quantity = portfolioStockDto.Quantity;
        item.AveragePrice = portfolioStockDto.PurchasePrice;
        item.UpdatedDate = DateTime.UtcNow;
        
        await _unitOfWork.PortfolioItems.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task RemoveStockFromPortfolioAsync(int portfolioStockId)
    {
        var item = await _unitOfWork.PortfolioItems.GetByIdAsync(portfolioStockId);
        if (item == null)
            throw new ArgumentException($"Portfolio stock with ID {portfolioStockId} not found");
            
        await _unitOfWork.PortfolioItems.DeleteAsync(item);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<decimal> GetPortfolioTotalValueAsync(int portfolioId)
    {
        var portfolio = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        if (portfolio == null)
            return 0;
            
        return portfolio.TotalValue;
    }
    
    public async Task<decimal> GetPortfolioProfitLossAsync(int portfolioId)
    {
        var portfolio = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        if (portfolio == null)
            return 0;
            
        return portfolio.TotalProfit;
    }
    
    public async Task<decimal> GetPortfolioProfitLossPercentageAsync(int portfolioId)
    {
        var portfolio = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        if (portfolio == null)
            return 0;
            
        return portfolio.TotalProfitPercentage;
    }
    
    public async Task UpdatePortfolioPricesAsync(int portfolioId)
    {
        // Get the portfolio with all items
        var portfolio = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(portfolioId);
        if (portfolio == null)
            return;
            
        // Update stock prices
        foreach (var item in portfolio.Items)
        {
            var stock = await _unitOfWork.Stocks.GetByIdAsync(item.StockId);
            if (stock != null)
            {
                // Here we would typically call an external API to get the latest price
                // For now, we'll just simulate a price update (random change)
                var random = new Random();
                var change = (decimal)(random.NextDouble() * 0.1 - 0.05); // -5% to +5%
                stock.CurrentPrice = stock.CurrentPrice * (1 + change);
                stock.DailyChangePercentage = change * 100;
                stock.LastUpdated = DateTime.UtcNow;
                
                await _unitOfWork.Stocks.UpdateAsync(stock);
            }
        }
        
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<PortfolioItemDto> AddStockToPortfolioAsync(int portfolioId, int stockId, decimal quantity, decimal price, string userId)
    {
        // Verify portfolio belongs to user
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this portfolio");
        }
        
        // Check if stock exists
        var stock = await _unitOfWork.Stocks.GetByIdAsync(stockId);
        if (stock == null)
        {
            throw new ArgumentException($"Stock with ID {stockId} not found");
        }
        
        // Check if the stock is already in the portfolio
        var existingItem = await _unitOfWork.PortfolioItems.GetPortfolioItemByStockAsync(portfolioId, stockId);
        
        if (existingItem != null)
        {
            // Update existing item
            var totalQuantity = existingItem.Quantity + quantity;
            var totalCost = (existingItem.AveragePrice * existingItem.Quantity) + (price * quantity);
            existingItem.AveragePrice = totalCost / totalQuantity;
            existingItem.Quantity = totalQuantity;
            existingItem.UpdatedDate = DateTime.UtcNow;
            
            await _unitOfWork.PortfolioItems.UpdateAsync(existingItem);
            await _unitOfWork.SaveChangesAsync();
            
            return _mapper.Map<PortfolioItemDto>(existingItem);
        }
        else
        {
            // Create new portfolio item
            var portfolioItem = new PortfolioItem
            {
                PortfolioId = portfolioId,
                StockId = stockId,
                Quantity = quantity,
                AveragePrice = price,
                CreatedDate = DateTime.UtcNow
            };
            
            await _unitOfWork.PortfolioItems.AddAsync(portfolioItem);
            
            // Add a transaction record
            var transaction = new Transaction
            {
                PortfolioId = portfolioId,
                StockId = stockId,
                Type = TransactionType.Buy,
                Price = price,
                Quantity = quantity,
                TransactionDate = DateTime.UtcNow,
                Notes = "Initial purchase"
            };
            
            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            
            // Fetch the complete item with stock for mapping
            var itemWithStock = await _unitOfWork.PortfolioItems.GetItemWithStockAsync(portfolioItem.Id);
            return _mapper.Map<PortfolioItemDto>(itemWithStock);
        }
    }
    
    public async Task UpdatePortfolioItemAsync(int portfolioItemId, decimal quantity, decimal price, string userId)
    {
        // Get the portfolio item with its portfolio
        var portfolioItem = await _unitOfWork.PortfolioItems.GetByIdAsync(portfolioItemId);
        if (portfolioItem == null)
        {
            throw new ArgumentException($"Portfolio item with ID {portfolioItemId} not found");
        }
        
        // Get the portfolio to verify ownership
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioItem.PortfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this portfolio item");
        }
        
        portfolioItem.Quantity = quantity;
        portfolioItem.AveragePrice = price;
        portfolioItem.UpdatedDate = DateTime.UtcNow;
        
        await _unitOfWork.PortfolioItems.UpdateAsync(portfolioItem);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task RemoveStockFromPortfolioAsync(int portfolioItemId, string userId)
    {
        // Get the portfolio item with its portfolio
        var portfolioItem = await _unitOfWork.PortfolioItems.GetByIdAsync(portfolioItemId);
        if (portfolioItem == null)
        {
            throw new ArgumentException($"Portfolio item with ID {portfolioItemId} not found");
        }
        
        // Get the portfolio to verify ownership
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioItem.PortfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this portfolio item");
        }
        
        await _unitOfWork.PortfolioItems.DeleteAsync(portfolioItem);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<TransactionDto> AddTransactionAsync(TransactionDto transactionDto, string userId)
    {
        // Verify portfolio belongs to user
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(transactionDto.PortfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this portfolio");
        }
        
        // Add the transaction
        var transaction = _mapper.Map<Transaction>(transactionDto);
        await _unitOfWork.Transactions.AddAsync(transaction);
        
        // Update the portfolio item
        var portfolioItem = await _unitOfWork.PortfolioItems.GetPortfolioItemByStockAsync(transactionDto.PortfolioId, transactionDto.StockId);
        
        if (portfolioItem == null && transactionDto.Type == TransactionType.Buy)
        {
            // If the stock is not in the portfolio and it's a buy transaction, create a new portfolio item
            portfolioItem = new PortfolioItem
            {
                PortfolioId = transactionDto.PortfolioId,
                StockId = transactionDto.StockId,
                Quantity = transactionDto.Quantity,
                AveragePrice = transactionDto.Price,
                CreatedDate = DateTime.UtcNow
            };
            
            await _unitOfWork.PortfolioItems.AddAsync(portfolioItem);
        }
        else if (portfolioItem != null)
        {
            // Update existing portfolio item
            if (transactionDto.Type == TransactionType.Buy)
            {
                // Calculate new average price
                var totalQuantity = portfolioItem.Quantity + transactionDto.Quantity;
                var totalCost = (portfolioItem.AveragePrice * portfolioItem.Quantity) + (transactionDto.Price * transactionDto.Quantity);
                
                portfolioItem.AveragePrice = totalCost / totalQuantity;
                portfolioItem.Quantity = totalQuantity;
            }
            else // Sell
            {
                if (portfolioItem.Quantity < transactionDto.Quantity)
                {
                    throw new InvalidOperationException("Cannot sell more shares than owned");
                }
                
                portfolioItem.Quantity -= transactionDto.Quantity;
                
                // If quantity becomes 0, remove the item
                if (portfolioItem.Quantity == 0)
                {
                    await _unitOfWork.PortfolioItems.DeleteAsync(portfolioItem);
                }
                else
                {
                    portfolioItem.UpdatedDate = DateTime.UtcNow;
                    await _unitOfWork.PortfolioItems.UpdateAsync(portfolioItem);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Cannot sell a stock that is not in the portfolio");
        }
        
        await _unitOfWork.SaveChangesAsync();
        
        return _mapper.Map<TransactionDto>(transaction);
    }
    
    public async Task<IEnumerable<TransactionDto>> GetPortfolioTransactionsAsync(int portfolioId, string userId)
    {
        // Verify portfolio belongs to user
        var portfolio = await _unitOfWork.Portfolios.GetByIdAsync(portfolioId);
        if (portfolio == null || portfolio.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this portfolio");
        }
        
        var transactions = await _unitOfWork.Transactions.GetTransactionsByPortfolioIdAsync(portfolioId);
        return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
    }
    
    public async Task<PortfolioWithStocksDto> GetPortfolioWithStocksAsync(int id)
    {
        var portfolio = await _unitOfWork.Portfolios.GetPortfolioWithItemsAndStocksAsync(id);
        if (portfolio == null)
            return new PortfolioWithStocksDto();
        
        var result = _mapper.Map<PortfolioWithStocksDto>(portfolio);
        
        // Calculate daily return as weighted average of stock daily changes
        if (result.TotalValue > 0 && result.Stocks.Any())
        {
            decimal dailyReturn = 0;
            foreach (var stock in result.Stocks)
            {
                // Each stock contributes to daily return proportional to its weight in portfolio
                dailyReturn += stock.DailyPriceChange * (stock.CurrentValue / result.TotalValue);
            }
            result.DailyReturn = dailyReturn;
        }
        else
        {
            result.DailyReturn = 0;
        }
        
        return result;
    }
    
    public Task<Dictionary<string, object>> GetPortfolioAnalysisAsync(int portfolioId, string userId)
    {
        // This method is now deprecated - use IPortfolioAnalysisService instead
        return Task.FromResult(new Dictionary<string, object>
        {
            { "message", "Please use the new PortfolioAnalysisService for real portfolio analysis" },
            { "deprecated", true }
        });
    }
    
    public Task<Dictionary<string, object>> GetPortfolioRecommendationsAsync(int portfolioId, string userId)
    {
        // This method is now deprecated - use IPortfolioAnalysisService instead
        return Task.FromResult(new Dictionary<string, object>
        {
            { "message", "Please use the new PortfolioAnalysisService for real portfolio recommendations" },
            { "deprecated", true }
        });
    }
} 