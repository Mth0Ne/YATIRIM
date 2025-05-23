using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;

namespace SmartBIST.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<Portfolio> Portfolios { get; set; } = null!;
    public DbSet<PortfolioItem> PortfolioItems { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<StockPriceHistory> StockPriceHistories { get; set; } = null!;
    public DbSet<TechnicalIndicator> TechnicalIndicators { get; set; } = null!;
    public DbSet<AIStockPrediction> AIStockPredictions { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Decimal tiplerin konfigürasyonlarını uygula
        ModelConfigurations.ApplyConfigurations(builder);
        
        // Configure entity relationships
        
        // Portfolio - ApplicationUser (One-to-Many)
        builder.Entity<Portfolio>()
            .HasOne(p => p.User)
            .WithMany(u => u.Portfolios)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Explicitly ignore IsActive property that doesn't exist in database schema
        builder.Entity<Portfolio>().Ignore("IsActive");
        
        // PortfolioItem - Portfolio (One-to-Many)
        builder.Entity<PortfolioItem>()
            .HasOne(pi => pi.Portfolio)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // PortfolioItem - Stock (One-to-Many)
        builder.Entity<PortfolioItem>()
            .HasOne(pi => pi.Stock)
            .WithMany(s => s.PortfolioItems)
            .HasForeignKey(pi => pi.StockId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Transaction - Portfolio (One-to-Many)
        builder.Entity<Transaction>()
            .HasOne(t => t.Portfolio)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Transaction - Stock (One-to-Many)
        builder.Entity<Transaction>()
            .HasOne(t => t.Stock)
            .WithMany(s => s.Transactions)
            .HasForeignKey(t => t.StockId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // StockPriceHistory - Stock (One-to-Many)
        builder.Entity<StockPriceHistory>()
            .HasOne(sph => sph.Stock)
            .WithMany(s => s.PriceHistory)
            .HasForeignKey(sph => sph.StockId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // TechnicalIndicator - Stock (One-to-Many)
        builder.Entity<TechnicalIndicator>()
            .HasOne(ti => ti.Stock)
            .WithMany(s => s.TechnicalIndicators)
            .HasForeignKey(ti => ti.StockId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // AIStockPrediction - Stock (One-to-Many)
        builder.Entity<AIStockPrediction>()
            .HasOne(p => p.Stock)
            .WithMany()
            .HasForeignKey(p => p.StockId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // AIStockPrediction - ApplicationUser (One-to-Many)
        builder.Entity<AIStockPrediction>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure indexes
        builder.Entity<Stock>()
            .HasIndex(s => s.Symbol)
            .IsUnique();
        
        builder.Entity<StockPriceHistory>()
            .HasIndex(sph => new { sph.StockId, sph.Date });
        
        builder.Entity<TechnicalIndicator>()
            .HasIndex(ti => new { ti.StockId, ti.Type, ti.Date });
    }
} 