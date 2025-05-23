using SmartBIST.Application.DTOs;
using System.Collections.Generic;

namespace SmartBIST.WebUI.Models;

public class HomeViewModel
{
    public List<StockDto> TopStocks { get; set; } = new();
    public List<StockDto> AllStocks { get; set; } = new();
    public Dictionary<string, object> MarketInsights { get; set; } = new();
} 