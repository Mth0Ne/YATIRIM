namespace SmartBIST.Core.Entities;

public class StockPriceHistory
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    
    // Navigation property
    public virtual Stock Stock { get; set; } = null!;
} 