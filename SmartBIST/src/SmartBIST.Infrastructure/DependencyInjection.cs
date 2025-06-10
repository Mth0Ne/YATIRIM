using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartBIST.Application.Services;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Data;
using SmartBIST.Infrastructure.Repositories;
using SmartBIST.Infrastructure.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;

namespace SmartBIST.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Veritabanı yapılandırması ve performans optimizasyonları
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    // Komut zaman aşımını artır
                    b.CommandTimeout(60);
                    // EF Core performans optimizasyonları
                    b.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }),
                // DbContext'i asla singleton veya uzun ömürlü servislerde kullanma
                ServiceLifetime.Scoped);

        // Redis yerine InMemoryCache kullan (daha hafif)
        services.AddMemoryCache();
        
        // UnitOfWork her zaman Scoped olmalı, asla Singleton olmamalı
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register the Individual Repositories
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IPortfolioItemRepository, PortfolioItemRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IStockPriceHistoryRepository, StockPriceHistoryRepository>();
        services.AddScoped<ITechnicalIndicatorRepository, TechnicalIndicatorRepository>();
        services.AddScoped<IAIStockPredictionRepository, AIStockPredictionRepository>();
        
        // Register Stock Scraper Service
        services.AddHttpClient<IStockScraperService, StockScraperService>();
        
        // Register Stock Prediction API Service
        services.AddHttpClient<IPredictionApiService, PredictionApiService>();
        
        // Register background service for stock data updates
        services.AddHostedService<StockDataUpdateService>();
        
        // Application Services
        services.AddScoped<ITechnicalIndicatorService, TechnicalIndicatorService>();
        services.AddScoped<IPortfolioAnalysisService, PortfolioAnalysisService>();
        
        // Real Technical Analysis Service (Clean Architecture)
        services.AddHttpClient<IRealTechnicalAnalysisService, RealTechnicalAnalysisService>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetService<IConfiguration>();
                var baseUrl = config?["PythonApi:BaseUrl"] ?? "http://localhost:5001";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(60); // Increase timeout for Python API
                client.DefaultRequestHeaders.Add("User-Agent", "SmartBIST/1.0");
            });
        
        // Email Service for Identity
        services.AddTransient<IEmailSender, EmailSender>();
        
        // Add Identity - Custom pages kullandığımız için AddDefaultUI() kaldırıldı
        services.AddIdentity<ApplicationUser, IdentityRole>(options => 
            {
                options.SignIn.RequireConfirmedAccount = false; // Hesap doğrulama gerekmiyor demo için
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        return services;
    }
} 