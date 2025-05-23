namespace SmartBIST.Core.Entities;

public class Portfolio
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<PortfolioItem> Items { get; set; } = new List<PortfolioItem>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    
    // Calculated properties (not stored in database)
    public decimal TotalValue => Items.Sum(i => i.CurrentValue);
    public decimal TotalCost => Items.Sum(i => i.TotalCost);
    public decimal TotalProfit => TotalValue - TotalCost;
    public decimal TotalProfitPercentage => TotalCost > 0 ? (TotalProfit / TotalCost) * 100 : 0;
} 