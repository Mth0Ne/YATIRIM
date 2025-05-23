using System;

namespace SmartBIST.Application.DTOs;

public class StockPriceHistoryDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    
    // Navigation props for UI
    public string StockSymbol { get; set; } = string.Empty;
} 