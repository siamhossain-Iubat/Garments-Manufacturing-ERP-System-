using System.Diagnostics;
using GManufacturingERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GManufacturingERP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // =====================================================
        // DEFAULT HOME ? LOGIN
        // =====================================================

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
        }

        // =====================================================
        // PRIVACY
        // =====================================================

        public IActionResult Privacy()
        {
            return View();
        }

        // =====================================================
        // ERROR
        // =====================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                });
        }
    }
}