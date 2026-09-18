using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Operations.Controllers
{
    [Area("Operations")]
    [Authorize(Roles = "OperationsManager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalApprovedOrders = await _context.SalesOrders
                .CountAsync(x => x.OrderStatus == "Approved");

            ViewBag.TotalProductionPlans = await _context.ProductionPlans
                .CountAsync();

            ViewBag.ActiveProductionPlans = await _context.ProductionPlans
                .CountAsync(x =>
                    x.ProductionStatus == "InProgress");

            ViewBag.CompletedProductionPlans = await _context.ProductionPlans
                .CountAsync(x =>
                    x.ProductionStatus == "Completed");

            return View();
        }
    }
}