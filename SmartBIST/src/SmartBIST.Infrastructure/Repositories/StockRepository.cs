using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace SmartBIST.Infrastructure.Repositories;

public class StockRepository : BaseRepository<Stock>, IStockRepository
{
    private readonly IMemoryCache _cache;
    private const int CACHE_DURATION_MINUTES = 15;

    public StockRepository(ApplicationDbContext context, IMemoryCache cache) : base(context)
    {
        _cache = cache;
    }

    public async Task<IEnumerable<Stock>> GetBySymbolsAsync(IEnumerable<string> symbols)
    {
        return await _dbContext.Stocks.Where(s => symbols.Contains(s.Symbol))
                          .Include(s => s.PriceHistory.OrderByDescending(ph => ph.Date).Take(30))
                          .ToListAsync();
    }

    public async Task<Stock?> GetBySymbolAsync(string symbol)
    {
        // Önbellek anahtarı
        string cacheKey = $"stock_{symbol}";
        
        // Önbellekte var mı kontrol et
        if (_cache.TryGetValue<Stock>(cacheKey, out var cachedStock))
        {
            return cachedStock;
        }
        
        // Veritabanından yükle
        var stock = await _dbContext.Stocks.Where(s => s.Symbol == symbol)
                               .Include(s => s.PriceHistory.OrderByDescending(ph => ph.Date).Take(30))
                               .FirstOrDefaultAsync();
                               
        // Varsa önbelleğe kaydet
        if (stock != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            
            _cache.Set(cacheKey, stock, cacheOptions);
        }
        
        return stock;
    }

    public async Task<IReadOnlyList<Stock>> GetStocksWithPaginationAsync(int pageIndex, int pageSize, string? searchTerm = null)
    {
        var query = _dbContext.Stocks.AsQueryable();
        
        // Arama terimi varsa filtrele
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToUpperInvariant();
            query = query.Where(s => s.Symbol.Contains(searchTerm) || s.Name.Contains(searchTerm));
        }
        
        // Sayfalama uygula
        return await query
            .OrderBy(s => s.Symbol)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<int> GetTotalStocksCountAsync(string? searchTerm = null)
    {
        var query = _dbContext.Stocks.AsQueryable();
        
        // Arama terimi varsa filtrele
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToUpperInvariant();
            query = query.Where(s => s.Symbol.Contains(searchTerm) || s.Name.Contains(searchTerm));
        }
        
        return await query.CountAsync();
    }
    
    public async Task<IReadOnlyList<Stock>> SearchStocksAsync(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
        {
            // Return all stocks if search term is empty or too short
            return await _dbContext.Stocks
                .OrderBy(s => s.Symbol)
                .ToListAsync();
        }
        
        // Search by both symbol and name, use ToUpper for case-insensitive search
        searchTerm = searchTerm.ToUpper();
        return await _dbContext.Stocks
            .Where(s => s.Symbol.ToUpper().Contains(searchTerm) || 
                  (s.Name != null && s.Name.ToUpper().Contains(searchTerm)))
            .OrderBy(s => s.Symbol)
            .ToListAsync();
    }
    
    public async Task<Stock?> GetStockWithPriceHistoryAsync(int id, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.Stocks
            .Include(s => s.PriceHistory.Where(ph => ph.Date >= startDate && ph.Date <= endDate))
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    
    public async Task<Stock?> GetStockWithTechnicalIndicatorsAsync(int id, IndicatorType indicatorType, DateTime date)
    {
        return await _dbContext.Stocks
            .Include(s => s.TechnicalIndicators.Where(ti => ti.Type == indicatorType && ti.Date == date))
            .FirstOrDefaultAsync(s => s.Id == id);
    }
} 