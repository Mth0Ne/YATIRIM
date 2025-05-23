namespace SmartBIST.Core.Entities;

public enum TransactionType
{
    Buy,
    Sell
}

public class Transaction
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalAmount => Price * Quantity;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
    public virtual Stock Stock { get; set; } = null!;
} 