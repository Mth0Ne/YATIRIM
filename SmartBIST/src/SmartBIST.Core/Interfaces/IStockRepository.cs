using SmartBIST.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SmartBIST.Core.Interfaces;

public interface IStockRepository : IRepository<Stock>
{
    Task<Stock?> GetBySymbolAsync(string symbol);
    Task<IEnumerable<Stock>> GetBySymbolsAsync(IEnumerable<string> symbols);
    Task<Stock?> GetStockWithPriceHistoryAsync(int id, DateTime startDate, DateTime endDate);
    Task<Stock?> GetStockWithTechnicalIndicatorsAsync(int id, IndicatorType indicatorType, DateTime date);
} 