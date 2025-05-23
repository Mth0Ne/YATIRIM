using SmartBIST.Application.DTOs;
using SmartBIST.Core.Entities;

namespace SmartBIST.Application.Services;

public interface IPredictionService
{
    Task<PredictionResultDto> GetPricePredictionAsync(PredictionRequestDto requestDto, string userId);
    Task<IEnumerable<PredictionResultDto>> GetUserPredictionsAsync(string userId);
    Task<IEnumerable<PredictionResultDto>> GetStockPredictionsAsync(int stockId, string userId);
    Task<PredictionResultDto?> GetPredictionByIdAsync(int id, string userId);
    Task<Dictionary<string, object>> GetMarketInsightsAsync();
} 