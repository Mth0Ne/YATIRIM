namespace SmartBIST.Application.DTOs;

public class StockDto
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal DailyChangePercentage { get; set; }
    public decimal Volume { get; set; }
    public decimal MarketCap { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? PBRatio { get; set; }
    public decimal? DividendYield { get; set; }
    public string Sector { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
} 