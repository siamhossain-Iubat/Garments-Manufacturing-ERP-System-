using GManufacturingERP.Data;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "SalesBuyerManager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var buyers = _context.Users
                .Where(x => x.UserType == "External");

            var totalBuyers = await buyers
                .CountAsync();

            var pendingBuyers = await buyers
                .CountAsync(x =>
                    x.AccountStatus == "PendingApproval");

            var approvedBuyers = await buyers
                .CountAsync(x =>
                    x.AccountStatus == "Approved" &&
                    x.IsActive);

            // সর্বশেষ ৫ জন buyer
            var recentBuyers = await buyers
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();

            var model = new SalesDashboardViewModel
            {
                TotalBuyers = totalBuyers,
                PendingBuyers = pendingBuyers,
                ApprovedBuyers = approvedBuyers,
                RecentBuyers = recentBuyers
            };

            return View(model);
        }
    }
}