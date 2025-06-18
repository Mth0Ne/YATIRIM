using SmartBIST.Application.DTOs;
using System.Collections.Generic;

namespace SmartBIST.WebUI.Models;

public class HomeViewModel
{
    public List<StockDto> TopStocks { get; set; } = new();
    public List<StockDto> AllStocks { get; set; } = new();
    public Dictionary<string, object> MarketInsights { get; set; } = new();
    
    // Portfolio Data
    public decimal TotalPortfolioValue { get; set; }
    public decimal TotalPortfolioChange { get; set; }
    public decimal TotalPortfolioChangePercentage { get; set; }
    public int ActivePositions { get; set; }
    public decimal TotalProfit { get; set; }
    public List<PortfolioDto> UserPortfolios { get; set; } = new();
} 