using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PurchaseOrderApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderApprovalsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendingOrders =
                await _context.PurchaseOrders
                    .Include(x => x.PurchaseRequisition)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.PurchaseOrderStatus == "Submitted")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

            var historyOrders =
                await _context.PurchaseOrders
                    .Include(x => x.PurchaseRequisition)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.PurchaseOrderStatus == "Approved" ||
                        x.PurchaseOrderStatus == "Rejected")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

            ViewBag.PendingOrders =
                pendingOrders;

            ViewBag.HistoryOrders =
                historyOrders;

            return View();
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var purchaseOrder =
                await _context.PurchaseOrders
                    .Include(x => x.PurchaseRequisition)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var purchaseOrder =
                await _context.PurchaseOrders
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            if (purchaseOrder.PurchaseOrderStatus !=
                "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted Purchase Orders can be approved.";

                return RedirectToAction(nameof(Index));
            }

            purchaseOrder.PurchaseOrderStatus =
                "Approved";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Purchase Order {purchaseOrder.PurchaseOrderNumber} approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REJECT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? rejectionReason)
        {
            var purchaseOrder =
                await _context.PurchaseOrders
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            if (purchaseOrder.PurchaseOrderStatus !=
                "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted Purchase Orders can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            purchaseOrder.PurchaseOrderStatus =
                "Rejected";

            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                purchaseOrder.Remarks =
                    $"Rejected by Admin: {rejectionReason}";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Purchase Order {purchaseOrder.PurchaseOrderNumber} rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}