using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartBIST.Application.DTOs;
using SmartBIST.Application.Services;
using SmartBIST.Core.Entities;
using SmartBIST.WebUI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartBIST.WebUI.Controllers;

[Authorize]
public class PortfolioController : Controller
{
    private readonly IPortfolioService _portfolioService;
    private readonly IStockService _stockService;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public PortfolioController(
        IPortfolioService portfolioService,
        IStockService stockService,
        UserManager<ApplicationUser> userManager)
    {
        _portfolioService = portfolioService;
        _stockService = stockService;
        _userManager = userManager;
    }
    
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);
        
        // Sadece aktif portföyleri göster
        var activePortfolios = portfolios.Where(p => p.IsActive).ToList();
        
        return View(activePortfolios);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioWithStocksAsync(id);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        return View(portfolio);
    }
    
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PortfolioDto portfolioDto)
    {
        // Debug: Log portfolio values
        Console.WriteLine($"PortfolioDto Name: {portfolioDto.Name}");
        Console.WriteLine($"PortfolioDto Description: {portfolioDto.Description}");
        Console.WriteLine($"PortfolioDto InvestmentStrategy: {portfolioDto.InvestmentStrategy}");
        Console.WriteLine($"PortfolioDto RiskLevel: {portfolioDto.RiskLevel}");
        Console.WriteLine($"PortfolioDto Type: {portfolioDto.Type}");
        Console.WriteLine($"PortfolioDto CurrencyCode: {portfolioDto.CurrencyCode}");
        
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "Kullanıcı kimliği alınamadı");
                return View(portfolioDto);
            }
            
            portfolioDto.UserId = userId;
            
            // Force RiskLevel to be within range
            if (portfolioDto.RiskLevel < 1 || portfolioDto.RiskLevel > 5)
            {
                portfolioDto.RiskLevel = 3; // Default to medium risk
            }
            
            // Ensure Type is valid
            if (string.IsNullOrEmpty(portfolioDto.Type))
            {
                portfolioDto.Type = "Normal";
            }
            
            // Ensure CurrencyCode is valid
            if (string.IsNullOrEmpty(portfolioDto.CurrencyCode))
            {
                portfolioDto.CurrencyCode = "TRY";
            }
            
            await _portfolioService.CreatePortfolioAsync(portfolioDto);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Portföy oluşturulurken bir hata oluştu: {ex.Message}");
            Console.WriteLine($"Exception creating portfolio: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        
        // If we get here, something went wrong
        if (!ModelState.IsValid)
        {
            // Log model state errors for debugging
            foreach (var modelStateKey in ModelState.Keys)
            {
                var modelStateVal = ModelState[modelStateKey];
                if (modelStateVal != null)
                {
                    foreach (var error in modelStateVal.Errors)
                    {
                        // Log or display error
                        var errorMessage = $"Key: {modelStateKey}, Error: {error.ErrorMessage}";
                        Console.WriteLine(errorMessage);
                    }
                }
            }
        }
        
        return View(portfolioDto);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        return View(portfolio);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PortfolioDto portfolioDto)
    {
        if (id != portfolioDto.Id)
        {
            return NotFound();
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingPortfolio = await _portfolioService.GetPortfolioByIdAsync(id);
        
        if (existingPortfolio == null || existingPortfolio.UserId != userId)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            portfolioDto.UserId = userId;
            await _portfolioService.UpdatePortfolioAsync(portfolioDto);
            return RedirectToAction(nameof(Index));
        }
        return View(portfolioDto);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        // Portföyü tamamen silmek yerine IsActive = false olarak işaretle
        portfolio.IsActive = false;
        await _portfolioService.UpdatePortfolioAsync(portfolio);
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        // Portföyü aktifleştir
        portfolio.IsActive = true;
        await _portfolioService.UpdatePortfolioAsync(portfolio);
        
        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> AddStock(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        // Sembol ve adlarıyla birlikte tüm hisseleri getir
        var stocks = await _stockService.GetAllStocksAsync();
        
        // Fiyatı sıfırdan büyük olan hisseleri filtrele
        var filteredStocks = stocks
            .Where(s => s != null && s.CurrentPrice > 0)
            .OrderBy(s => s.Symbol)
            .ToList();
        
        var viewModel = new AddStockViewModel
        {
            PortfolioId = id,
            Stocks = filteredStocks
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStock(int PortfolioId, string StockSymbol, decimal Quantity, decimal AverageCost, string? _formattedAverageCost = null)
    {
        // If we have the formatted cost value from JS, use it instead
        if (!string.IsNullOrEmpty(_formattedAverageCost) && decimal.TryParse(_formattedAverageCost, 
            System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out decimal formattedCost))
        {
            AverageCost = formattedCost;
        }
        
        if (string.IsNullOrEmpty(StockSymbol) || Quantity <= 0 || AverageCost <= 0)
        {
            TempData["ErrorMessage"] = "Lütfen tüm alanları geçerli değerlerle doldurun.";
            return RedirectToAction(nameof(Details), new { id = PortfolioId });
        }
        
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var portfolio = await _portfolioService.GetPortfolioWithStocksAsync(PortfolioId);
            
            if (portfolio == null || portfolio.UserId != userId)
            {
                return NotFound();
            }
            
            // StockSymbol parametresinden StockId'yi al
            var stock = await _stockService.GetStockBySymbolAsync(StockSymbol);
            
            if (stock == null)
            {
                TempData["ErrorMessage"] = $"{StockSymbol} sembolüne sahip hisse bulunamadı.";
                return RedirectToAction(nameof(Details), new { id = PortfolioId });
            }
            
            // Check if this stock already exists in the portfolio
            var existingStock = portfolio.Stocks.FirstOrDefault(s => s.Symbol == StockSymbol);
            
            if (existingStock != null)
            {
                // If the stock already exists, calculate a new weighted average cost
                decimal totalShares = existingStock.Quantity + Quantity;
                decimal totalCost = (existingStock.Quantity * existingStock.AverageCost) + (Quantity * AverageCost);
                decimal newAverageCost = totalCost / totalShares;
                
                // Update the existing stock entry
                var updateDto = new PortfolioStockDto
                {
                    Id = existingStock.Id,
                    PortfolioId = PortfolioId,
                    StockId = stock.Id,
                    StockSymbol = stock.Symbol,
                    StockName = stock.Name,
                    Quantity = totalShares,
                    PurchasePrice = newAverageCost,
                    PurchaseDate = DateTime.Now
                };
                
                await _portfolioService.UpdatePortfolioStockAsync(updateDto);
                TempData["SuccessMessage"] = $"{StockSymbol} hissesi portföyde güncellendi. Yeni adet: {totalShares}, Yeni ortalama maliyet: {newAverageCost.ToString("N2", new System.Globalization.CultureInfo("tr-TR"))}";
            }
            else
            {
                // If the stock doesn't exist, create a new entry
                var portfolioStockDto = new PortfolioStockDto
                {
                    PortfolioId = PortfolioId,
                    StockId = stock.Id,
                    StockSymbol = stock.Symbol,
                    StockName = stock.Name,
                    Quantity = Quantity,
                    PurchasePrice = AverageCost,
                    PurchaseDate = DateTime.Now
                };
                
                await _portfolioService.CreatePortfolioStockAsync(portfolioStockDto);
                TempData["SuccessMessage"] = $"{StockSymbol} hissesi portföye başarıyla eklendi.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Hisse eklenirken bir hata oluştu: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Details), new { id = PortfolioId });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(PortfolioStockDto portfolioStockDto)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioStockDto.PortfolioId);
            
            if (portfolio == null || portfolio.UserId != userId)
            {
                return NotFound();
            }
            
            await _portfolioService.UpdatePortfolioStockAsync(portfolioStockDto);
        }
        
        return RedirectToAction(nameof(Details), new { id = portfolioStockDto.PortfolioId });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStock(int id, int portfolioId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
        
        if (portfolio == null || portfolio.UserId != userId)
        {
            return NotFound();
        }
        
        await _portfolioService.RemoveStockFromPortfolioAsync(id);
        return RedirectToAction(nameof(Details), new { id = portfolioId });
    }
    
    [HttpGet]
    public async Task<IActionResult> SearchStocks(string term)
    {
        try
        {
            // Log the search term for debugging
            Console.WriteLine($"SearchStocks called with term: '{term}'");
            
            // Eğer term boş veya kısa ise tüm hisseleri getir
            IEnumerable<StockDto> stocks;
            if (string.IsNullOrEmpty(term))
            {
                stocks = await _stockService.GetAllStocksAsync();
                Console.WriteLine($"GetAllStocksAsync returned {stocks.Count()} stocks");
            }
            else
            {
                // Sembol bazlı arama yap
                stocks = await _stockService.SearchStocksAsync(term);
                Console.WriteLine($"SearchStocksAsync with term '{term}' returned {stocks.Count()} stocks");
            }
            
            // Stok fiyatı 0'dan büyük olanları filtrele
            var filteredStocks = stocks
                .Where(s => s != null && s.CurrentPrice > 0)
                .OrderBy(s => s.Symbol)
                .Take(50) // Performans için sonuçları sınırla
                .ToList();
            
            Console.WriteLine($"After filtering, returning {filteredStocks.Count} stocks");
            
            // select2 için gereken format { id: "ID", text: "Display Text" }
            var results = filteredStocks.Select(s => new { 
                id = s.Symbol, // Sembol'ü ID olarak kullan
                text = $"{s.Symbol} - {s.CurrentPrice:N2} ₺ ({s.DailyChangePercentage:F2}%)" 
            });
            
            return Json(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SearchStocks: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            
            // Boş JSON dizisi döndür
            return Json(new object[0]);
        }
    }
} 