using SmartBIST.Core.Entities;

namespace SmartBIST.Application.DTOs;

public class PredictionRequestDto
{
    public int StockId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PredictionModel ModelType { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
} 