using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBIST.Application.DTOs;

namespace SmartBIST.WebUI.Models
{
    public class TechnicalAnalysisViewModel
    {
        // Form properties
        public List<SelectListItem> AvailableStocks { get; set; } = new();
        public string? Symbol { get; set; }
        public int Period { get; set; } = 90;
        
        // Data properties
        public TechnicalAnalysisResultDto? TechnicalAnalysis { get; set; }
        public PriceHistoryResultDto? PriceHistory { get; set; }
        
        // State properties
        public string? ErrorMessage { get; set; }
        public bool IsDataLoaded { get; set; }
        
        // Helper properties for the view
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasData => IsDataLoaded && TechnicalAnalysis != null && PriceHistory != null;
    }
} 