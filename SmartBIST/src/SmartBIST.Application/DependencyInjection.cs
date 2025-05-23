using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartBIST.Application.Services;
using AutoMapper;
using FluentValidation.AspNetCore;
using SmartBIST.Application.Validators;
using SmartBIST.Application.DTOs;

namespace SmartBIST.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Register Application Services
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPredictionService, PredictionService>();
        
        // FluentValidation için validatörleri ekle
        services.AddValidatorsFromAssemblyContaining<PortfolioDtoValidator>();
        services.AddFluentValidationAutoValidation();
        
        // Validate edilecek DTO'ları ekle
        services.AddScoped<IValidator<PortfolioDto>, PortfolioDtoValidator>();
        services.AddScoped<IValidator<StockDto>, StockDtoValidator>();
        services.AddScoped<IValidator<TransactionDto>, TransactionDtoValidator>();
        
        return services;
    }
} 