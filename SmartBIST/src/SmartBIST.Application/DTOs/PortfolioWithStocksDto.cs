using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartBIST.Application.DTOs
{
    public class PortfolioWithStocksDto
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; }
        
        public string Type { get; set; } = string.Empty;
        
        public string CurrencyCode { get; set; } = "TRY";
        
        public string UserId { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; }
        
        public decimal TotalValue { get; set; }
        
        public decimal TotalCost { get; set; }
        
        public decimal TotalReturn { get; set; }
        
        public decimal DailyReturn { get; set; }
        
        public List<PortfolioStockItemDto> Stocks { get; set; } = new List<PortfolioStockItemDto>();
    }
    
    public class PortfolioStockItemDto
    {
        public int Id { get; set; }
        
        public string Symbol { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public decimal Quantity { get; set; }
        
        public decimal AverageCost { get; set; }
        
        public decimal CurrentPrice { get; set; }
        
        public decimal CurrentValue => Quantity * CurrentPrice;
        
        public decimal TotalCost => Quantity * AverageCost;
        
        public decimal ProfitLoss => CurrentValue - TotalCost;
        
        public decimal DailyPriceChange { get; set; }
        
        public decimal TotalReturn { get; set; }
        
        public decimal PortfolioPercentage { get; set; }
    }
} 