using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;

namespace SmartBIST.Infrastructure.Data;

public static class ModelConfigurations
{
    public static void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        // Stock entity konfigürasyonu
        modelBuilder.Entity<Stock>()
            .Property(s => s.CurrentPrice)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.DailyChangePercentage)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.Volume)
            .HasColumnType("decimal(18,2)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.MarketCap)
            .HasColumnType("decimal(18,2)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.PERatio)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.PBRatio)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<Stock>()
            .Property(s => s.DividendYield)
            .HasColumnType("decimal(18,4)");
            
        // StockPriceHistory entity konfigürasyonu
        modelBuilder.Entity<StockPriceHistory>()
            .Property(h => h.Open)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<StockPriceHistory>()
            .Property(h => h.High)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<StockPriceHistory>()
            .Property(h => h.Low)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<StockPriceHistory>()
            .Property(h => h.Close)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<StockPriceHistory>()
            .Property(h => h.Volume)
            .HasColumnType("decimal(18,2)");
            
        // PortfolioItem entity konfigürasyonu
        modelBuilder.Entity<PortfolioItem>()
            .Property(p => p.AveragePrice)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<PortfolioItem>()
            .Property(p => p.Quantity)
            .HasColumnType("decimal(18,4)");
            
        // Transaction entity konfigürasyonu
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Price)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Quantity)
            .HasColumnType("decimal(18,4)");
            
        // AIStockPrediction entity konfigürasyonu
        modelBuilder.Entity<AIStockPrediction>()
            .Property(a => a.Accuracy)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<AIStockPrediction>()
            .Property(a => a.CurrentPrice)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<AIStockPrediction>()
            .Property(a => a.PredictedPrice)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<AIStockPrediction>()
            .Property(a => a.PriceChange)
            .HasColumnType("decimal(18,4)");
            
        modelBuilder.Entity<AIStockPrediction>()
            .Property(a => a.PercentChange)
            .HasColumnType("decimal(18,4)");
    }
} 