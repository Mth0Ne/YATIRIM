namespace SmartBIST.Core.Entities;

public class PortfolioItem
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
    public virtual Stock Stock { get; set; } = null!;
    
    // Calculated properties (not stored in database)
    public decimal TotalCost => AveragePrice * Quantity;
    public decimal CurrentValue => Stock.CurrentPrice * Quantity;
    public decimal ProfitLoss => CurrentValue - TotalCost;
    public decimal ProfitLossPercentage => TotalCost > 0 ? (ProfitLoss / TotalCost) * 100 : 0;
} 