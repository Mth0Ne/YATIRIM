using SmartBIST.Application.DTOs;
using SmartBIST.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SmartBIST.WebUI.Models;

public class PortfolioDetailsViewModel
{
    public PortfolioDto Portfolio { get; set; } = new PortfolioDto();
    public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    public Dictionary<string, object> Analysis { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Recommendations { get; set; } = new Dictionary<string, object>();
}

public class AddStockViewModel
{
    public int PortfolioId { get; set; }
    
    [Required]
    [Display(Name = "Stock")]
    public int StockId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    public List<StockDto> Stocks { get; set; } = new List<StockDto>();
}

public class AddTransactionViewModel
{
    public int PortfolioId { get; set; }
    
    [Required]
    [Display(Name = "Stock")]
    public int StockId { get; set; }
    
    [Required]
    [Display(Name = "Transaction Type")]
    public TransactionType Type { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
    
    [Required]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
} 