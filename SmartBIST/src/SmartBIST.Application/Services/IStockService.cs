using SmartBIST.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartBIST.Application.Services;

public interface IStockService
{
    Task<IEnumerable<StockDto>> GetAllStocksAsync();
    Task<StockDto?> GetStockByIdAsync(int id);
    Task<StockDto?> GetStockBySymbolAsync(string symbol);
    Task<IEnumerable<StockPriceHistoryDto>> GetStockPriceHistoryAsync(int stockId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<StockDto>> SearchStocksAsync(string query);
    Task<bool> UpdateStockPricesAsync();
    Task<Dictionary<string, object>> GetTechnicalIndicatorsAsync(int stockId, string indicator, Dictionary<string, string> parameters);
    Task<bool> AddStockAsync(CreateStockDto stockDto);
    Task<bool> UpdateStockAsync(UpdateStockDto stockDto);
    Task DeleteStockAsync(int id);
    Task EnsureStocksInitializedAsync();
} 