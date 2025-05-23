using System;

namespace SmartBIST.Application.DTOs;

public class AddStockToPortfolioDto
{
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Notes { get; set; }
}

public class SellStockFromPortfolioDto
{
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Notes { get; set; }
} 