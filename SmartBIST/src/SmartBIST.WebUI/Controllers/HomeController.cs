using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartBIST.Application.DTOs;
using SmartBIST.Application.Services;
using SmartBIST.Core.Entities;
using SmartBIST.Core.Interfaces;
using SmartBIST.WebUI.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace SmartBIST.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IStockService _stockService;
    private readonly IPredictionService _predictionService;
    private readonly IPortfolioService _portfolioService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        IStockService stockService,
        IPredictionService predictionService,
        IPortfolioService portfolioService,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _stockService = stockService;
        _predictionService = predictionService;
        _portfolioService = portfolioService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        try 
        {
            // Tüm hisseleri getir
            var allStocks = await _stockService.GetAllStocksAsync();
            
            // Boş olmayan ve fiyatı olan hisseleri filtrele
            var stocks = allStocks
                .Where(s => s != null && s.CurrentPrice > 0)
                .OrderByDescending(s => s.DailyChangePercentage)
                .ToList();
            
            var viewModel = new HomeViewModel
            {
                // En iyi performans gösterenler (ilk 5)
                TopStocks = stocks.Take(5).ToList(),
                
                // Tüm hisse listesi
                AllStocks = stocks,
                
                MarketInsights = new Dictionary<string, object>()
            };

            // Kullanıcı giriş yapmışsa portföy verilerini çek
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);
                        var activePortfolios = portfolios.Where(p => p.IsActive).ToList();
                        
                        viewModel.UserPortfolios = activePortfolios;
                        viewModel.TotalPortfolioValue = activePortfolios.Sum(p => p.TotalValue);
                        viewModel.TotalProfit = activePortfolios.Sum(p => p.TotalProfit);
                        viewModel.ActivePositions = activePortfolios.Sum(p => p.StockCount);
                        
                        // Günlük değişim hesaplama (örnek - daha detaylı hesaplama gerekebilir)
                        if (viewModel.TotalPortfolioValue > 0)
                        {
                            var totalCost = activePortfolios.Sum(p => p.TotalCost);
                            if (totalCost > 0)
                            {
                                viewModel.TotalPortfolioChangePercentage = (viewModel.TotalProfit / totalCost) * 100;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kullanıcı portföy verileri çekilirken hata oluştu: {UserId}", userId);
                        // Portföy hatası ana sayfa yüklenmesini engellemez
                    }
                }
            }
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ana sayfa yüklenirken hata oluştu");
            return View(new HomeViewModel 
            { 
                TopStocks = new List<StockDto>(),
                AllStocks = new List<StockDto>(),
                MarketInsights = new Dictionary<string, object> { ["error"] = "Veriler yüklenirken bir hata oluştu" }
            });
        }
    }

    public IActionResult Gizlilik()
    {
        return View("Privacy");
    }

    [Authorize]
    public async Task<IActionResult> UserSettings()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new UserSettingsViewModel
        {
            Name = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserSettings(UserSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        user.UserName = model.Name;
        user.PhoneNumber = model.Phone;

        if (user.Email != model.Email)
        {
            user.Email = model.Email;
            user.EmailConfirmed = false;
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Kullanıcı bilgileriniz başarıyla güncellendi.";
            return RedirectToAction(nameof(UserSettings));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
