using System.Security.Claims;
using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MY INVOICES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var invoices = await _context.Invoices
                .Include(x => x.SalesOrder)
                .Include(x => x.Delivery)
                .Where(x =>
                    x.SalesOrder != null &&
                    x.SalesOrder.BuyerId == userId)
                .OrderByDescending(
                    x => x.InvoiceDate)
                .ToListAsync();

            return View(invoices);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var invoice = await _context.Invoices
                .Include(x => x.SalesOrder)
                .Include(x => x.Delivery)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.SalesOrder != null &&
                    x.SalesOrder.BuyerId == userId);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }
    }
}