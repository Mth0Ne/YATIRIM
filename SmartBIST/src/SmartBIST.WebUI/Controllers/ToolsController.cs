using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartBIST.WebUI.Models;

namespace SmartBIST.WebUI.Controllers;

[Authorize]
public class ToolsController : Controller
{
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(ILogger<ToolsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Calculator()
    {
        return View();
    }

    public IActionResult TechnicalAnalysis()
    {
        return View();
    }
} 