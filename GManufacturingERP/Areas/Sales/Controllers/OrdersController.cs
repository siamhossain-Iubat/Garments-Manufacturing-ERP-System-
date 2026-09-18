using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "SalesBuyerManager")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // All Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.SalesOrders
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // Pending Orders
        public async Task<IActionResult> Pending()
        {
            var orders = await _context.SalesOrders
                .Where(x => x.OrderStatus == "Pending")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View("Index", orders);
        }

        // Order Details
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Approve Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id,
            string? salesRemarks)
        {
            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending orders can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.Id });
            }

            order.OrderStatus = "Approved";
            order.SalesRemarks = salesRemarks;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Order {order.OrderNumber} approved successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = order.Id });
        }

        // Reject Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? salesRemarks)
        {
            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending orders can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.Id });
            }

            order.OrderStatus = "Rejected";
            order.SalesRemarks = salesRemarks;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Order {order.OrderNumber} rejected.";

            return RedirectToAction(
                nameof(Details),
                new { id = order.Id });
        }
    }
}