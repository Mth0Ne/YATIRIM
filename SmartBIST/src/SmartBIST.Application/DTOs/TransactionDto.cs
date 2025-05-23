using System;
using System.ComponentModel.DataAnnotations;
using SmartBIST.Core.Entities;

namespace SmartBIST.Application.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Portföy ID'si gereklidir")]
    public int PortfolioId { get; set; }
    
    [Required(ErrorMessage = "Hisse senedi ID'si gereklidir")]
    public int StockId { get; set; }
    
    public string StockSymbol { get; set; } = string.Empty;
    
    public string StockName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "İşlem tipi gereklidir")]
    public TransactionType Type { get; set; }
    
    [Required(ErrorMessage = "İşlem fiyatı gereklidir")]
    [Range(0.01, double.MaxValue, ErrorMessage = "İşlem fiyatı 0'dan büyük olmalıdır")]
    public decimal Price { get; set; }
    
    [Required(ErrorMessage = "Miktar gereklidir")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "İşlem tarihi gereklidir")]
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    
    [Range(0, double.MaxValue, ErrorMessage = "Komisyon 0 veya daha büyük olmalıdır")]
    public decimal Commission { get; set; } = 0;
    
    public string? Notes { get; set; }
    
    // Doğrulama için yardımcı
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal CurrentPortfolioQuantity { get; set; }
    
    // Calculated properties
    public decimal TotalAmount => Type == TransactionType.Buy 
        ? Price * Quantity * -1 
        : Price * Quantity;
} 