using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using System.Collections.Concurrent;
using System.Transactions;

namespace SmartBIST.Application.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockScraperService _stockScraperService;
    private readonly IMapper _mapper;
    private readonly ILogger<StockService> _logger;
    private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
    private static bool _isInitialized = false;
    
    // Common Turkish stock symbols
    private static readonly string[] _defaultTurkishStockSymbols = new[]
    {
        "THYAO", "GARAN", "ASELS", "YKBNK", "BIMAS", "SISE", "EREGL", "TUPRS", "KCHOL", "SAHOL",
        "PGSUS", "AKBNK", "HEKTS", "TTKOM", "VAKBN", "FROTO", "ISMEN", "TCELL", "ARCLK", "TAVHL"
    };
    
    public StockService(
        IUnitOfWork unitOfWork,
        IStockScraperService stockScraperService,
        IMapper mapper,
        ILogger<StockService> logger)
    {
        _unitOfWork = unitOfWork;
        _stockScraperService = stockScraperService;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<IEnumerable<StockDto>> GetAllStocksAsync()
    {
        try
        {
            _logger.LogInformation("Getting all stocks from database");
            var stocks = await _unitOfWork.Stocks.GetAllAsync();
            
            // If no stocks in database, initialize with real data from Yahoo Finance
            if (stocks == null || !stocks.Any())
            {
                _logger.LogInformation("No stocks found in database, initializing with real data");
                await EnsureStocksInitializedAsync();
                stocks = await _unitOfWork.Stocks.GetAllAsync();
                _logger.LogInformation($"After initialization, found {stocks.Count()} stocks");
            }
            else if (stocks.Any(s => (DateTime.UtcNow - s.LastUpdated).TotalHours > 24))
            {
                // Asenkron çağrıları doğrudan yapmak yerine fire-and-forget kullanmayı kaldırıyoruz
                _logger.LogInformation("Some stocks are older than 24 hours, updating prices");
                await UpdateStockPricesAsync();
            }
            
            var dtoList = _mapper.Map<IEnumerable<StockDto>>(stocks).ToList();
            _logger.LogInformation($"Returning {dtoList.Count} stocks");
            
            // Filter out any null items (should not happen, but just in case)
            if (dtoList.Any(s => s == null))
            {
                _logger.LogWarning("Found null items in stock list, filtering them out");
                dtoList = dtoList.Where(s => s != null).ToList();
            }
            
            return dtoList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stocks");
            return Enumerable.Empty<StockDto>();
        }
    }
    
    public async Task EnsureStocksInitializedAsync()
    {
        // Make sure we're only doing this once
        if (_isInitialized) return;
        
        try
        {
            await _initializationLock.WaitAsync();
            
            if (_isInitialized) return; // Check again in case another thread got here first
            
            _logger.LogInformation("Initializing stocks database...");
            
            // Get real stock data from Yahoo Finance
            var existingStocksCount = await _unitOfWork.Stocks.CountAsync();
            _logger.LogInformation($"Current stock count: {existingStocksCount}");
            
            if (existingStocksCount == 0)
            {
                _logger.LogInformation("No stocks found in database, fetching from API");
                
                try
                {
                    // Try to fetch current prices from the scraper service
                    var stocksData = await _stockScraperService.GetCurrentStockPricesAsync();
                    _logger.LogInformation($"Retrieved {stocksData.Count()} stocks from API");
                    
                    if (stocksData.Any())
                    {
                        // Add each stock to the database
                        foreach (var stockData in stocksData)
                        {
                            await _unitOfWork.Stocks.AddAsync(stockData);
                        }
                        
                        // Save changes to the database
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation($"Added {stocksData.Count()} stocks to the database");
                    }
                    else
                    {
                        _logger.LogWarning("No stocks returned from API, using default Turkish stocks");
                        await InitializeWithDefaultStocksAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching stock data from API, using default Turkish stocks");
                    await InitializeWithDefaultStocksAsync();
                }
            }
            
            _isInitialized = true;
            _logger.LogInformation("Stock initialization completed successfully");
        }
        finally
        {
            _initializationLock.Release();
        }
    }
    
    private async Task InitializeWithDefaultStocksAsync()
    {
        _logger.LogInformation("Initializing with default Turkish stocks");
        var random = new Random();
        var now = DateTime.UtcNow;
        
        foreach (var symbol in _defaultTurkishStockSymbols)
        {
            // Generate some fake data for testing
            decimal price = (decimal)(random.NextDouble() * 100 + 10); // 10-110 TL
            decimal change = (decimal)(random.NextDouble() * 10 - 5);  // -5% to +5%
            
            var stock = new Stock
            {
                Symbol = symbol,
                Name = $"{symbol} A.Ş.",
                CurrentPrice = price,
                DailyChangePercentage = change,
                Volume = random.Next(1000, 1000000),
                LastUpdated = now
            };
            
            await _unitOfWork.Stocks.AddAsync(stock);
        }
        
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation($"Added {_defaultTurkishStockSymbols.Length} default stocks to the database");
    }
    
    public async Task<StockDto?> GetStockByIdAsync(int id)
    {
        try
        {
            var stock = await _unitOfWork.Stocks.GetByIdAsync(id);
            if (stock == null)
            {
                _logger.LogWarning("ID {id} olan hisse senedi bulunamadı", id);
                return null;
            }
            
            return _mapper.Map<StockDto>(stock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock {Id}", id);
            return null;
        }
    }
    
    public async Task<StockDto?> GetStockBySymbolAsync(string symbol)
    {
        try
        {
            var stock = await _unitOfWork.Stocks.GetBySymbolAsync(symbol);
            if (stock == null)
            {
                _logger.LogWarning("Symbol {symbol} olan hisse senedi bulunamadı", symbol);
                return null;
            }
            
            return _mapper.Map<StockDto>(stock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock with symbol {Symbol}", symbol);
            return null;
        }
    }
    
    public async Task<IEnumerable<StockPriceHistoryDto>> GetStockPriceHistoryAsync(int stockId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var history = await _unitOfWork.StockPriceHistories.GetPriceHistoryByDateRangeAsync(stockId, startDate, endDate);
            return _mapper.Map<IEnumerable<StockPriceHistoryDto>>(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for stock {StockId}", stockId);
            return Enumerable.Empty<StockPriceHistoryDto>();
        }
    }
    
    public async Task<IEnumerable<StockDto>> SearchStocksAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogInformation("Empty search query, returning all stocks");
                return await GetAllStocksAsync();
            }
            
            _logger.LogInformation("Searching for stocks with query: {Query}", query);
            query = query.ToUpperInvariant().Trim();
            
            // First, get all stocks
            var allStocks = await _unitOfWork.Stocks.GetAllAsync();
            
            // Filter stocks that match the query
            var filteredStocks = allStocks
                .Where(s => s.Symbol.Contains(query) || 
                           (s.Name != null && s.Name.Contains(query)))
                .ToList();
                
            _logger.LogInformation("Found {Count} stocks matching query", filteredStocks.Count);
            
            return _mapper.Map<IEnumerable<StockDto>>(filteredStocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for stocks with query: {Query}", query);
            return Enumerable.Empty<StockDto>();
        }
    }
    
    public async Task<bool> UpdateStockPricesAsync()
    {
        try
        {
            // Get all the stock data in one call
            var stockData = await _stockScraperService.GetCurrentStockPricesAsync();
            if (stockData == null || !stockData.Any())
            {
                _logger.LogWarning("No stock data received from scraper service");
                return false;
            }
            
            // Get all symbols we need to update
            var symbolsToUpdate = stockData.Select(s => s.Symbol).ToArray();
            if (symbolsToUpdate.Length == 0)
            {
                _logger.LogWarning("No valid stock symbols to update");
                return false;
            }
            
            _logger.LogInformation("Updating prices for {Count} stocks", symbolsToUpdate.Length);
            
            // Load all existing stocks in a single query - avoid multiple GetBySymbol calls
            var existingStocks = (await _unitOfWork.Stocks.GetBySymbolsAsync(symbolsToUpdate)).ToList();
            if (!existingStocks.Any())
            {
                _logger.LogWarning("None of the stocks to update were found in the database");
                return false;
            }
            
            var currentDate = DateTime.Now.Date;
            
            // Create dictionaries for faster lookups
            var stocksBySymbol = existingStocks.ToDictionary(s => s.Symbol, s => s);
            
            // Get all existing price histories for today in one query
            var stockIds = existingStocks.Select(s => s.Id).ToArray();
            var existingHistories = (await _unitOfWork.StockPriceHistories.GetByStocksAndDateAsync(stockIds, currentDate))
                .ToDictionary(h => h.StockId, h => h);
            
            // Prepare lists for batch updates
            var stocksToUpdate = new List<Stock>();
            var newHistories = new List<StockPriceHistory>();
            var historiesToUpdate = new List<StockPriceHistory>();
            
            // Process updates without making further DB calls
            foreach (var stockInfo in stockData)
            {
                // Skip if we don't have this stock
                if (!stocksBySymbol.TryGetValue(stockInfo.Symbol, out var existingStock))
                    continue;
                
                // Update the stock information
                existingStock.CurrentPrice = stockInfo.CurrentPrice;
                existingStock.DailyChangePercentage = stockInfo.DailyChangePercentage;
                existingStock.Volume = stockInfo.Volume;
                existingStock.LastUpdated = DateTime.Now;
                stocksToUpdate.Add(existingStock);
                
                // Handle price history
                if (existingHistories.TryGetValue(existingStock.Id, out var existingHistory))
                {
                    // Update existing history
                    existingHistory.High = Math.Max(existingHistory.High, stockInfo.CurrentPrice);
                    existingHistory.Low = Math.Min(existingHistory.Low, stockInfo.CurrentPrice);
                    existingHistory.Close = stockInfo.CurrentPrice;
                    existingHistory.Volume = stockInfo.Volume;
                    historiesToUpdate.Add(existingHistory);
                }
                else
                {
                    // Create new history
                    var newHistory = new StockPriceHistory
                    {
                        StockId = existingStock.Id,
                        Date = currentDate,
                        Open = stockInfo.CurrentPrice,
                        High = stockInfo.CurrentPrice,
                        Low = stockInfo.CurrentPrice,
                        Close = stockInfo.CurrentPrice,
                        Volume = stockInfo.Volume
                    };
                    newHistories.Add(newHistory);
                }
            }
            
            // Process all updates in batches
            foreach (var stock in stocksToUpdate)
            {
                await _unitOfWork.Stocks.UpdateAsync(stock);
            }
            
            foreach (var history in historiesToUpdate)
            {
                await _unitOfWork.StockPriceHistories.UpdateAsync(history);
            }
            
            foreach (var history in newHistories)
            {
                await _unitOfWork.StockPriceHistories.AddAsync(history);
            }
            
            // Save all changes at once
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Updated {Count} stocks", stocksToUpdate.Count);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock prices");
            return false;
        }
    }
    
    public async Task<Dictionary<string, object>> GetTechnicalIndicatorsAsync(int stockId, string indicator, Dictionary<string, string> parameters)
    {
        // This method is now deprecated - use ITechnicalIndicatorService instead
        return new Dictionary<string, object>
        {
            ["message"] = "Please use the new TechnicalIndicatorService for real technical analysis",
            ["deprecated"] = true
        };
    }
    
    public async Task<bool> AddStockAsync(CreateStockDto stockDto)
    {
        try
        {
            // Check if stock with same symbol already exists
            var existingStock = await _unitOfWork.Stocks.GetBySymbolAsync(stockDto.Symbol);
            if (existingStock != null)
            {
                return false;
            }
            
            var stock = new Stock
            {
                Symbol = stockDto.Symbol,
                Name = stockDto.Name,
                Description = stockDto.Description,
                CurrentPrice = stockDto.CurrentPrice,
                DailyChangePercentage = 0, // Default value
                Volume = 0, // Default value
                MarketCap = 0, // Default value
                LastUpdated = DateTime.Now
            };
            
            await _unitOfWork.Stocks.AddAsync(stock);
            await _unitOfWork.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock {Symbol}", stockDto.Symbol);
            return false;
        }
    }
    
    public async Task<bool> UpdateStockAsync(UpdateStockDto stockDto)
    {
        try
        {
            var stock = await _unitOfWork.Stocks.GetByIdAsync(stockDto.Id);
            if (stock == null)
            {
                return false;
            }
            
            stock.Name = stockDto.Name;
            stock.Description = stockDto.Description;
            stock.LastUpdated = DateTime.Now;
            
            await _unitOfWork.Stocks.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock {Id}", stockDto.Id);
            return false;
        }
    }
    
    public async Task DeleteStockAsync(int id)
    {
        var stock = await _unitOfWork.Stocks.GetByIdAsync(id);
        if (stock == null)
        {
            throw new ArgumentException($"Stock with ID {id} not found");
        }
        
        await _unitOfWork.Stocks.DeleteAsync(stock);
        await _unitOfWork.SaveChangesAsync();
    }
} 