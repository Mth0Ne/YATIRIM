namespace SmartBIST.Core.Entities;

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DailyChangePercentage { get; set; }
    public decimal Volume { get; set; }
    public decimal MarketCap { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? PBRatio { get; set; }
    public decimal? DividendYield { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<StockPriceHistory> PriceHistory { get; set; } = new List<StockPriceHistory>();
    public virtual ICollection<TechnicalIndicator> TechnicalIndicators { get; set; } = new List<TechnicalIndicator>();
} 