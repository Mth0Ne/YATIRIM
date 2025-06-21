using SmartBIST.Application.DTOs;

namespace SmartBIST.Application.Services;

public interface IDataMiningAnalysisService
{
    Task<DataMiningAnalysisResultDto> GetDataMiningAnalysisAsync(string symbol, int periodDays = 90, bool includePredictions = true);
} 