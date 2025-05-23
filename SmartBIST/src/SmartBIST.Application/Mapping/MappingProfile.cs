using AutoMapper;
using Newtonsoft.Json;
using SmartBIST.Application.DTOs;
using SmartBIST.Core.Entities;
using System.Text.Json;

namespace SmartBIST.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Stock mappings
        CreateMap<Stock, StockDto>().ReverseMap();
        
        // Portfolio mappings
        CreateMap<Portfolio, PortfolioDto>()
            .ForMember(dest => dest.TotalValue, opt => opt.Ignore())
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.TotalProfit, opt => opt.MapFrom(src => src.TotalProfit))
            .ForMember(dest => dest.TotalProfitPercentage, opt => opt.MapFrom(src => src.TotalProfitPercentage))
            .ForMember(dest => dest.StockCount, opt => opt.MapFrom(src => src.Items.Count));
            
        CreateMap<PortfolioDto, Portfolio>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
        
        // PortfolioWithStocks mapping
        CreateMap<Portfolio, PortfolioWithStocksDto>()
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => src.TotalValue))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.TotalReturn, opt => opt.MapFrom(src => src.TotalCost > 0 ? (src.TotalValue - src.TotalCost) / src.TotalCost : 0))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Stocks, opt => opt.MapFrom(src => src.Items));
            
        // PortfolioItem to PortfolioStockItemDto mapping
        CreateMap<PortfolioItem, PortfolioStockItemDto>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Stock.Symbol))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Stock.Name))
            .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.Stock.CurrentPrice))
            .ForMember(dest => dest.AverageCost, opt => opt.MapFrom(src => src.AveragePrice))
            .ForMember(dest => dest.CurrentValue, opt => opt.MapFrom(src => src.CurrentValue))
            .ForMember(dest => dest.DailyPriceChange, opt => opt.MapFrom(src => src.Stock.DailyChangePercentage / 100))
            .ForMember(dest => dest.TotalReturn, opt => opt.MapFrom(src => 
                src.AveragePrice > 0 ? (src.Stock.CurrentPrice - src.AveragePrice) / src.AveragePrice : 0
            ))
            .ForMember(dest => dest.PortfolioPercentage, opt => opt.MapFrom(src => 
                src.Portfolio.TotalValue > 0 ? src.CurrentValue / src.Portfolio.TotalValue : 0
            ));
        
        // PortfolioItem mappings
        CreateMap<PortfolioItem, PortfolioItemDto>()
            .ForMember(dest => dest.StockSymbol, opt => opt.MapFrom(src => src.Stock.Symbol))
            .ForMember(dest => dest.StockName, opt => opt.MapFrom(src => src.Stock.Name))
            .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.Stock.CurrentPrice))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.CurrentValue, opt => opt.MapFrom(src => src.CurrentValue))
            .ForMember(dest => dest.ProfitLoss, opt => opt.MapFrom(src => src.ProfitLoss))
            .ForMember(dest => dest.ProfitLossPercentage, opt => opt.MapFrom(src => src.ProfitLossPercentage));
            
        CreateMap<PortfolioItemDto, PortfolioItem>()
            .ForMember(dest => dest.Stock, opt => opt.Ignore())
            .ForMember(dest => dest.Portfolio, opt => opt.Ignore());
        
        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.StockSymbol, opt => opt.MapFrom(src => src.Stock.Symbol))
            .ForMember(dest => dest.StockName, opt => opt.MapFrom(src => src.Stock.Name));
            
        CreateMap<TransactionDto, Transaction>()
            .ForMember(dest => dest.Stock, opt => opt.Ignore())
            .ForMember(dest => dest.Portfolio, opt => opt.Ignore());
        
        // AI Prediction mappings
        CreateMap<AIStockPrediction, PredictionResultDto>()
            .ForMember(dest => dest.Parameters, opt => opt.MapFrom(src => 
                DeserializeParameters(src.Parameters)))
            .ForMember(dest => dest.PredictionData, opt => opt.MapFrom(src => 
                DeserializePredictionData(src.PredictionData)))
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => true));
                
        CreateMap<PredictionResultDto, AIStockPrediction>()
            .ForMember(dest => dest.Parameters, opt => opt.MapFrom(src => 
                SerializeParameters(src.Parameters)))
            .ForMember(dest => dest.PredictionData, opt => opt.MapFrom(src => 
                SerializePredictionData(src.PredictionData)))
            .ForMember(dest => dest.Stock, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }

    private static Dictionary<string, string>? DeserializeParameters(string parameters)
    {
        if (string.IsNullOrEmpty(parameters))
            return null;
        
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(parameters);
    }

    private static Dictionary<string, object>? DeserializePredictionData(string predictionData)
    {
        if (string.IsNullOrEmpty(predictionData))
            return null;
        
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(predictionData, options);
    }

    private static string SerializeParameters(Dictionary<string, string>? parameters)
    {
        if (parameters == null)
            return "{}";
        
        return System.Text.Json.JsonSerializer.Serialize(parameters);
    }

    private static string SerializePredictionData(Dictionary<string, object>? predictionData)
    {
        if (predictionData == null)
            return "{}";
        
        return System.Text.Json.JsonSerializer.Serialize(predictionData);
    }
} 