using System;
using System.ComponentModel.DataAnnotations;

namespace SmartBIST.Application.DTOs;

public class PortfolioItemDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Portföy ID'si gereklidir")]
    public int PortfolioId { get; set; }
    
    [Required(ErrorMessage = "Hisse senedi ID'si gereklidir")]
    public int StockId { get; set; }
    
    public string StockSymbol { get; set; } = string.Empty;
    
    public string StockName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Miktar gereklidir")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Ortalama fiyat gereklidir")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Ortalama fiyat 0'dan büyük olmalıdır")]
    public decimal AveragePrice { get; set; }
    
    public decimal CurrentPrice { get; set; }
    
    // Calculated fields
    public decimal TotalCost => Quantity * AveragePrice;
    
    public decimal CurrentValue => Quantity * CurrentPrice;
    
    public decimal ProfitLoss => CurrentValue - TotalCost;
    
    public decimal ProfitLossPercentage => TotalCost != 0 ? (ProfitLoss / TotalCost) * 100 : 0;
} 