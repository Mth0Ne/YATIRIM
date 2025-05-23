using SmartBIST.Application.DTOs;

namespace SmartBIST.Application.Services;

public interface IRealTechnicalAnalysisService
{
    Task<TechnicalAnalysisResultDto> GetTechnicalAnalysisAsync(string symbol, int periodDays = 90);
    Task<PriceHistoryResultDto> GetPriceHistoryAsync(string symbol, int periodDays = 90);
} 