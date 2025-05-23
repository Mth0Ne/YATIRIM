using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartBIST.Application.DTOs;

public class PortfolioDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Portföy adı gereklidir")]
    [StringLength(100, ErrorMessage = "Portföy adı en fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(100, ErrorMessage = "Yatırım stratejisi en fazla 100 karakter olabilir")]
    public string InvestmentStrategy { get; set; } = string.Empty;
    
    [Range(1, 5, ErrorMessage = "Risk seviyesi 1 ile 5 arasında olmalıdır")]
    public int RiskLevel { get; set; } = 3;
    
    public string Type { get; set; } = "Normal";
    
    public string CurrencyCode { get; set; } = "TRY";
    
    [Required(ErrorMessage = "Kullanıcı ID'si gereklidir")]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    public DateTime? ModifiedDate { get; set; }
    
    // Portfolio analytics
    public int StockCount { get; set; }
    
    public decimal TotalValue { get; set; }
    
    public decimal TotalCost { get; set; }
    
    public decimal TotalProfit => TotalValue - TotalCost;
    
    public decimal TotalProfitPercentage => TotalCost != 0 ? (TotalProfit / TotalCost) * 100 : 0;
    
    // Navigation property for related stocks in portfolio
    public List<PortfolioStockDto> Stocks { get; set; } = new List<PortfolioStockDto>();
} 