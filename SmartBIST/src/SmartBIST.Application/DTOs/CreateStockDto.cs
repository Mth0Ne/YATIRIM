using System;

namespace SmartBIST.Application.DTOs;

public class CreateStockDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CurrentPrice { get; set; }
} 