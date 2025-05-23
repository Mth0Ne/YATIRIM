using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class PortfolioRepository : BaseRepository<Portfolio>, IPortfolioRepository
{
    public PortfolioRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<IReadOnlyList<Portfolio>> GetUserPortfoliosAsync(string userId)
    {
        // Önce tüm portföyleri getir
        var portfolios = await _dbContext.Portfolios
            .Where(p => p.UserId == userId)
            .Select(p => new Portfolio
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                IsActive = p.IsActive
            })
            .ToListAsync();
            
        // Ardından her portföy için hisseleri getir
        foreach (var portfolio in portfolios)
        {
            var items = await _dbContext.PortfolioItems
                .Include(pi => pi.Stock)
                .Where(pi => pi.PortfolioId == portfolio.Id)
                .ToListAsync();
                
            // Portföye hisseleri ekle
            foreach (var item in items)
            {
                portfolio.Items.Add(item);
            }
        }
        
        return portfolios;
    }
    
    public async Task<Portfolio?> GetPortfolioWithItemsAsync(int id)
    {
        // IsActive özelliğini sorgulara dahil ediyoruz
        var portfolio = await _dbContext.Portfolios
            .Where(p => p.Id == id)
            .Select(p => new Portfolio
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();
            
        if (portfolio != null)
        {
            // Load portfolio items separately
            var items = await _dbContext.PortfolioItems
                .Where(pi => pi.PortfolioId == id)
                .ToListAsync();
                
            // Add items to the portfolio manually
            foreach (var item in items)
            {
                portfolio.Items.Add(item);
            }
        }
        
        return portfolio;
    }
    
    public async Task<Portfolio?> GetPortfolioWithItemsAndStocksAsync(int id)
    {
        // IsActive özelliğini dahil ediyoruz
        var portfolio = await _dbContext.Portfolios
            .Where(p => p.Id == id)
            .Select(p => new Portfolio
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();
            
        if (portfolio != null)
        {
            // Load portfolio items and stocks separately
            var items = await _dbContext.PortfolioItems
                .Include(pi => pi.Stock)
                .Where(pi => pi.PortfolioId == id)
                .ToListAsync();
                
            // Add items to the portfolio manually
            foreach (var item in items)
            {
                portfolio.Items.Add(item);
            }
        }
        
        return portfolio;
    }
    
    public async Task<Portfolio?> GetPortfolioWithItemsAndTransactionsAsync(int id)
    {
        // IsActive özelliğini dahil ediyoruz
        var portfolio = await _dbContext.Portfolios
            .Where(p => p.Id == id)
            .Select(p => new Portfolio
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();
            
        if (portfolio != null)
        {
            // Load portfolio items separately
            var items = await _dbContext.PortfolioItems
                .Where(pi => pi.PortfolioId == id)
                .ToListAsync();
                
            // Load transactions separately
            var transactions = await _dbContext.Transactions
                .Include(t => t.Stock)
                .Where(t => t.PortfolioId == id)
                .ToListAsync();
                
            // Add items and transactions to the portfolio manually
            foreach (var item in items)
            {
                portfolio.Items.Add(item);
            }
            
            foreach (var transaction in transactions)
            {
                portfolio.Transactions.Add(transaction);
            }
        }
        
        return portfolio;
    }
    
    public async Task<Portfolio?> GetCompletePortfolioAsync(int id)
    {
        // IsActive özelliğini dahil ediyoruz
        var portfolio = await _dbContext.Portfolios
            .Where(p => p.Id == id)
            .Select(p => new Portfolio
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();
            
        if (portfolio != null)
        {
            // Load user separately
            var user = await _dbContext.Users
                .Where(u => u.Id == portfolio.UserId)
                .FirstOrDefaultAsync();
                
            // Load portfolio items and stocks separately
            var items = await _dbContext.PortfolioItems
                .Include(pi => pi.Stock)
                .Where(pi => pi.PortfolioId == id)
                .ToListAsync();
                
            // Load transactions separately
            var transactions = await _dbContext.Transactions
                .Include(t => t.Stock)
                .Where(t => t.PortfolioId == id)
                .ToListAsync();
                
            // Add data to the portfolio manually
            if (user != null)
            {
                portfolio.User = user;
            }
            
            foreach (var item in items)
            {
                portfolio.Items.Add(item);
            }
            
            foreach (var transaction in transactions)
            {
                portfolio.Transactions.Add(transaction);
            }
        }
        
        return portfolio;
    }
} 