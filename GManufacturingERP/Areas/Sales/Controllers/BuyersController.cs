using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "SalesBuyerManager")]
    public class BuyersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuyersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // All buyers + search
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Users
                .Where(x => x.UserType == "External")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.CompanyName != null &&
                     x.CompanyName.Contains(search)) ||

                    x.FullName.Contains(search) ||

                    (x.Email != null &&
                     x.Email.Contains(search)) ||

                    (x.Country != null &&
                     x.Country.Contains(search)));
            }

            var buyers = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;

            return View(buyers);
        }

        // Buyer details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var buyer = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserType == "External");

            if (buyer == null)
            {
                return NotFound();
            }

            return View(buyer);
        }
    }
}