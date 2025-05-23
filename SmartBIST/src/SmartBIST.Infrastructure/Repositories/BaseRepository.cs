using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;

namespace SmartBIST.Infrastructure.Repositories;

public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _dbContext;
    
    public BaseRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbContext.Set<T>().FindAsync(id);
    }
    
    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        // IsActive özelliğini dahil ediyoruz (veritabanı güncellendiği için)
        if (typeof(T).Name == "Portfolio")
        {
            // Portfolio entity için özel işleme
            var portfolios = await _dbContext.Set<T>()
                .Select(p => new 
                {
                    Id = EF.Property<int>(p, "Id"),
                    Name = EF.Property<string>(p, "Name"),
                    Description = EF.Property<string>(p, "Description"),
                    UserId = EF.Property<string>(p, "UserId"),
                    CreatedDate = EF.Property<DateTime>(p, "CreatedDate"),
                    UpdatedDate = EF.Property<DateTime?>(p, "UpdatedDate"),
                    IsActive = EF.Property<bool>(p, "IsActive")
                })
                .ToListAsync();
            
            // T'ye dönüştür
            var result = new List<T>();
            foreach (var item in portfolios)
            {
                var portfolio = Activator.CreateInstance<T>();
                
                typeof(T).GetProperty("Id")?.SetValue(portfolio, item.Id);
                typeof(T).GetProperty("Name")?.SetValue(portfolio, item.Name);
                typeof(T).GetProperty("Description")?.SetValue(portfolio, item.Description);
                typeof(T).GetProperty("UserId")?.SetValue(portfolio, item.UserId);
                typeof(T).GetProperty("CreatedDate")?.SetValue(portfolio, item.CreatedDate);
                typeof(T).GetProperty("UpdatedDate")?.SetValue(portfolio, item.UpdatedDate);
                typeof(T).GetProperty("IsActive")?.SetValue(portfolio, item.IsActive);
                
                result.Add(portfolio);
            }
            
            return result;
        }
        
        return await _dbContext.Set<T>().ToListAsync();
    }
    
    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().Where(predicate).ToListAsync();
    }
    
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbContext.Set<T>().AddAsync(entity);
        return entity;
    }
    
    public virtual Task UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }
    
    public virtual Task DeleteAsync(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }
    
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return await _dbContext.Set<T>().CountAsync();
        
        return await _dbContext.Set<T>().CountAsync(predicate);
    }
    
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().AnyAsync(predicate);
    }
} 