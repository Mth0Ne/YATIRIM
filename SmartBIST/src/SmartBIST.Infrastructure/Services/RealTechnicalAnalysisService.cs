using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Application.Services;
using System.Text.Json;

namespace SmartBIST.Infrastructure.Services;

public class RealTechnicalAnalysisService : IRealTechnicalAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealTechnicalAnalysisService> _logger;
    private readonly string _baseUrl;

    public RealTechnicalAnalysisService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<RealTechnicalAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PythonApi:BaseUrl"] ?? "http://localhost:5001";
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<TechnicalAnalysisResultDto> GetTechnicalAnalysisAsync(string symbol, int periodDays = 90)
    {
        try
        {
            _logger.LogInformation("Fetching technical analysis for {Symbol} with period {PeriodDays}", symbol, periodDays);
            
            var url = $"{_baseUrl}/technical-analysis/{symbol}?period_days={periodDays}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Python API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Python API hatası: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var pythonResponse = JsonSerializer.Deserialize<PythonTechnicalAnalysisResponse>(jsonContent, options);
            
            if (pythonResponse == null)
            {
                throw new InvalidOperationException("Python API'den geçersiz yanıt alındı");
            }

            return MapToDto(pythonResponse);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Technical analysis request timed out for {Symbol}", symbol);
            throw new InvalidOperationException("İstek zaman aşımına uğradı");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during technical analysis for {Symbol}", symbol);
            throw new InvalidOperationException("Python API'ye bağlanılamadı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting technical analysis for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<PriceHistoryResultDto> GetPriceHistoryAsync(string symbol, int periodDays = 90)
    {
        try
        {
            _logger.LogInformation("Fetching price history for {Symbol} with period {PeriodDays}", symbol, periodDays);
            
            var url = $"{_baseUrl}/price-history/{symbol}?period_days={periodDays}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Python API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Python API hatası: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var pythonResponse = JsonSerializer.Deserialize<PythonPriceHistoryResponse>(jsonContent, options);
            
            if (pythonResponse == null)
            {
                throw new InvalidOperationException("Python API'den geçersiz yanıt alındı");
            }

            return new PriceHistoryResultDto
            {
                Symbol = pythonResponse.Symbol,
                DataPoints = pythonResponse.DataPoints,
                PriceHistory = pythonResponse.PriceHistory.Select(p => new PriceDataDto
                {
                    Date = DateTime.Parse(p.Date),
                    Open = p.Open,
                    High = p.High,
                    Low = p.Low,
                    Close = p.Close,
                    Volume = p.Volume
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for {Symbol}", symbol);
            throw;
        }
    }

    private static TechnicalAnalysisResultDto MapToDto(PythonTechnicalAnalysisResponse pythonResponse)
    {
        return new TechnicalAnalysisResultDto
        {
            Symbol = pythonResponse.Symbol,
            CurrentPrice = pythonResponse.CurrentPrice,
            AnalysisDate = DateTime.Parse(pythonResponse.AnalysisDate),
            PeriodDays = pythonResponse.PeriodDays,
            DataPoints = pythonResponse.DataPoints,
            Indicators = pythonResponse.Indicators,
            Signals = new TechnicalSignalsDto
            {
                IndividualSignals = pythonResponse.Signals.IndividualSignals,
                OverallSignal = pythonResponse.Signals.OverallSignal,
                SignalStrength = pythonResponse.Signals.SignalStrength,
                BuySignals = pythonResponse.Signals.BuySignals,
                SellSignals = pythonResponse.Signals.SellSignals,
                NeutralSignals = pythonResponse.Signals.NeutralSignals
            },
            PriceHistory = pythonResponse.PriceHistory.Select(p => new PriceDataDto
            {
                Date = DateTime.Parse(p.Date),
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            }).ToList()
        };
    }
}

// Python API Response Models (Infrastructure concern)
public class PythonTechnicalAnalysisResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public string AnalysisDate { get; set; } = string.Empty;
    public int PeriodDays { get; set; }
    public int DataPoints { get; set; }
    public Dictionary<string, object> Indicators { get; set; } = new();
    public PythonSignalsResponse Signals { get; set; } = new();
    public List<PythonPriceData> PriceHistory { get; set; } = new();
}

public class PythonSignalsResponse
{
    public Dictionary<string, string> IndividualSignals { get; set; } = new();
    public string OverallSignal { get; set; } = string.Empty;
    public double SignalStrength { get; set; }
    public int BuySignals { get; set; }
    public int SellSignals { get; set; }
    public int NeutralSignals { get; set; }
}

public class PythonPriceData
{
    public string Date { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class PythonPriceHistoryResponse
{
    public string Symbol { get; set; } = string.Empty;
    public List<PythonPriceData> PriceHistory { get; set; } = new();
    public int DataPoints { get; set; }
} 