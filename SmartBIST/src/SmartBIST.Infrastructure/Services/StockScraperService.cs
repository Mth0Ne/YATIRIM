using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Globalization;

namespace SmartBIST.Infrastructure.Services;

public class StockScraperService : IStockScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockScraperService> _logger;
    private readonly string _mynetBaseUrl = "https://finans.mynet.com/borsa/hisseler/";

    public StockScraperService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<StockScraperService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure default headers
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public async Task<IEnumerable<Stock>> GetCurrentStockPricesAsync()
    {
        try
        {
            _logger.LogInformation($"Fetching stock data from Mynet at {DateTime.Now}");
            
            var result = new List<Stock>();
            
            // Send HTTP request
            var response = await _httpClient.GetAsync(_mynetBaseUrl);
            response.EnsureSuccessStatusCode();
            
            // Parse HTML content
            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            
            // Find all stock links - similar to Python example
            var stockLinks = doc.DocumentNode.SelectNodes("//table//a[contains(@href, '/borsa/hisseler/')]");
            
            if (stockLinks != null)
            {
                foreach (var link in stockLinks)
                {
                    try
                    {
                        // Get the parent row
                        var row = link.SelectSingleNode("ancestor::tr");
                        if (row == null) continue;
                        
                        // Get all cells in the row
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 5) continue;
                        
                        // Extract information about the entire HTML row for debugging
                        _logger.LogDebug($"Full HTML row for {link.InnerText.Trim()}: {row.OuterHtml}");
                        
                        // Log all cell contents for debugging
                        _logger.LogDebug($"Found {cells.Count} cells for {link.InnerText.Trim()}:");
                        for (int i = 0; i < cells.Count; i++)
                        {
                            _logger.LogDebug($"  Cell {i}: '{cells[i].InnerText.Trim()}' HTML: {cells[i].OuterHtml}");
                        }
                        
                        // Extract data
                        var stockName = link.InnerText.Trim();
                        
                        // Extract stock code from URL
                        var href = link.GetAttributeValue("href", "");
                        var parts = href.Split('/');
                        if (parts.Length < 3)
                        {
                            _logger.LogWarning($"Invalid href format: {href}");
                            continue;
                        }
                        
                        var stockCodePart = parts[parts.Length - 2];
                        if (string.IsNullOrWhiteSpace(stockCodePart))
                        {
                            _logger.LogWarning($"Empty stock code part in href: {href}");
                            continue;
                        }
                        
                        var stockCode = stockCodePart.Split('-')[0].ToUpper();
                        
                        // Make sure we have a valid stock code
                        if (string.IsNullOrWhiteSpace(stockCode))
                        {
                            _logger.LogWarning($"Empty stock code extracted from href: {href}");
                            continue;
                        }
                        
                        // Try to intelligently determine which cells contain which data
                        // Mynet Finans web sitesinin yapısını analiz edelim ve en çok benzeyen değerleri bulalım
                        decimal price = 0;
                        decimal change = 0;
                        decimal volume = 0;
                        
                        // Tüm hücrelerde değerleri arayalım
                        for (int i = 0; i < cells.Count; i++)
                        {
                            string cellValue = cells[i].InnerText.Trim();
                            
                            // Boş hücreler atlayalım
                            if (string.IsNullOrWhiteSpace(cellValue))
                                continue;
                                
                            // Yüzde işareti varsa muhtemelen değişim yüzdesidir
                            if (cellValue.Contains("%"))
                            {
                                string cleanValue = cellValue.Replace("%", "").Trim();
                                
                                // Ondalık ayırıcıyı doğru şekilde işle
                                // Türkçe formatındaki virgülü noktaya çevir
                                cleanValue = cleanValue.Replace(',', '.');
                                
                                if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedChange))
                                {
                                    // Değişim değerini direkt olarak kullan, bölme işlemi yapma
                                    change = parsedChange;
                                    _logger.LogDebug($"Found change value in cell {i}: {change} (original: {cellValue}, cleaned: {cleanValue})");
                                }
                                continue;
                            }
                            
                            // Büyük sayılar muhtemelen fiyattır (1'den büyük)
                            if (decimal.TryParse(cellValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                            {
                                if (price == 0 && parsed > 0)
                                {
                                    price = parsed;
                                    _logger.LogDebug($"Found price value in cell {i}: {price}");
                                    continue;
                                }
                            }
                            
                            // M, K, B gibi harfler içeren veya çok büyük sayılar muhtemelen hacimdir
                            if (cellValue.Contains("M") || cellValue.Contains("K") || cellValue.Contains("B") || 
                                (decimal.TryParse(cellValue.Replace('.', ' ').Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVolume) && parsedVolume > 1000000))
                            {
                                // Hacim değerini temizle ve dönüştür
                                string volumeStr = Regex.Replace(cellValue, @"[^\d.]", "");
                                if (decimal.TryParse(volumeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out volume))
                                {
                                    // Milyon, milyar gibi gösterimleri kontrol et
                                    if (cellValue.Contains("M")) volume *= 1000000;
                                    else if (cellValue.Contains("K")) volume *= 1000;
                                    else if (cellValue.Contains("B")) volume *= 1000000000;
                                    
                                    _logger.LogDebug($"Found volume value in cell {i}: {volume}");
                                }
                            }
                        }
                        
                        // Eğer hala değerler bulamadıysak, varsayılan endeksleri kullan
                        if (price == 0 && cells.Count > 2)
                        {
                            string priceText = cells[2].InnerText.Trim();
                            price = TryParseDecimal(priceText, $"fallback price for {stockCode}");
                            _logger.LogDebug($"Using fallback price column (2): {price}");
                        }
                        
                        if (change == 0 && cells.Count > 3)
                        {
                            string changeText = cells[3].InnerText.Trim();
                            change = TryParseDecimal(changeText, $"fallback change for {stockCode}");
                            _logger.LogDebug($"Using fallback change column (3): {change}");
                        }
                        
                        if (volume == 0 && cells.Count > 5)
                        {
                            string volumeText = cells[5].InnerText.Trim();
                            volume = TryParseDecimal(volumeText, $"fallback volume for {stockCode}");
                            _logger.LogDebug($"Using fallback volume column (5): {volume}");
                        }
                        
                        // Log the extracted and parsed stock data for debugging
                        _logger.LogInformation($"Final extracted stock data: Code={stockCode}, Name={stockName}, Price={price}, Change={change}, Volume={volume}");
                        
                        // Create stock object with correct mapping
                        result.Add(new Stock
                        {
                            Symbol = stockCode,
                            Name = stockName,
                            CurrentPrice = price,  // Using the value from the correct column
                            DailyChangePercentage = change,  // Using the value from the correct column
                            Volume = volume,  // Using the value from the correct column
                            LastUpdated = DateTime.Now
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing a stock row");
                    }
                }
            }
            
            // Final check - remove any stocks with empty symbols
            var filteredResult = result.Where(s => !string.IsNullOrWhiteSpace(s.Symbol)).ToList();
            if (filteredResult.Count < result.Count)
            {
                _logger.LogWarning($"Removed {result.Count - filteredResult.Count} stocks with empty symbols");
            }
            
            _logger.LogInformation($"Successfully retrieved data for {filteredResult.Count} stocks");
            return filteredResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock data from Mynet");
            return Enumerable.Empty<Stock>();
        }
    }
    
    public async Task<IEnumerable<Stock>> GetCurrentStockPricesAsync(string[] symbols)
    {
        try
        {
            _logger.LogInformation($"Fetching stock data for specific symbols: {string.Join(", ", symbols)}");
            
            var allStocks = await GetCurrentStockPricesAsync();
            
            // Filter by the requested symbols
            return allStocks.Where(s => symbols.Contains(s.Symbol, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock data for specific symbols");
            return Enumerable.Empty<Stock>();
        }
    }
    
    public async Task<IEnumerable<StockPriceHistory>> GetHistoricalDataAsync(string symbol)
    {
        try
        {
            // Build the URL for the specific stock
            var stockUrl = $"{_mynetBaseUrl}{symbol.ToLowerInvariant()}/";
            
            var response = await _httpClient.GetAsync(stockUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            
            // This is a placeholder - actual implementation would parse the historical data
            // from the stock detail page or from another API endpoint
            
            var result = new List<StockPriceHistory>();
            
            _logger.LogInformation($"Retrieved historical data for {symbol}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical data for {Symbol} from Mynet", symbol);
            return Enumerable.Empty<StockPriceHistory>();
        }
    }

    // Helper method to try multiple approaches to parse decimal values
    private decimal TryParseDecimal(string text, string fieldDescription)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning($"Empty text for {fieldDescription}");
            return 0;
        }
        
        // Log the input for debugging
        _logger.LogDebug($"Attempting to parse '{text}' for {fieldDescription}");
        
        // Yüzde işareti temizleme (eğer varsa) - önce bunu yapıyoruz
        string cleanText = text;
        bool isPercentage = text.Contains("%");
        if (isPercentage)
        {
            cleanText = text.Replace("%", "").Trim();
            _logger.LogDebug($"Removed percentage sign: '{text}' -> '{cleanText}'");
        }
        
        // Virgülü noktaya çevir - Türkçe'de ondalık ayırıcı virgüldür
        cleanText = cleanText.Replace(',', '.');
        
        // Try standard parse with invariant culture (which uses dot as decimal separator)
        if (decimal.TryParse(cleanText, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            _logger.LogDebug($"Successfully parsed: {cleanText} -> {result}");
            return result;
        }
        
        // Turkish culture which uses comma as decimal separator
        if (decimal.TryParse(text, NumberStyles.Any, new CultureInfo("tr-TR"), out result))
        {
            _logger.LogDebug($"Parsed using Turkish culture: {text} -> {result}");
            return result;
        }
        
        // Try replacing dot with comma and parse with Turkish culture
        cleanText = text.Replace('.', ',');
        if (decimal.TryParse(cleanText, NumberStyles.Any, new CultureInfo("tr-TR"), out result))
        {
            _logger.LogDebug($"Parsed after replacing dot with comma: {cleanText} -> {result}");
            return result;
        }
        
        // Log the failure
        _logger.LogWarning($"Failed to parse '{text}' as decimal for {fieldDescription}");
        return 0;
    }
} 