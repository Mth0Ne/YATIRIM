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

namespace SmartBIST.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IStockService _stockService;
    private readonly IPredictionService _predictionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        IStockService stockService,
        IPredictionService predictionService,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _stockService = stockService;
        _predictionService = predictionService;
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
