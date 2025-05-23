using System;
using System.Collections.Generic;
using SmartBIST.Core.Entities;

namespace SmartBIST.Application.DTOs;

public class PredictionResultDto
{
    // Temel bilgiler
    public int Id { get; set; }
    public int StockId { get; set; }
    public string StockSymbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public PredictionModel ModelType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime PredictionStartDate { get; set; }
    public DateTime PredictionEndDate { get; set; }
    
    // API'den gelen tahmin verileri
    public decimal PredictedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PercentChange { get; set; }
    public string PredictionDate { get; set; } = string.Empty;
    public string LastCloseDate { get; set; } = string.Empty;
    public int DataPoints { get; set; }
    
    // Parametreler ve ham API verisi
    public Dictionary<string, string>? Parameters { get; set; }
    public Dictionary<string, object>? PredictionData { get; set; }
    
    // Performans metriği
    public decimal Accuracy { get; set; }
    
    // API hatası durumunda
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
} 