using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DeliveryApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeliveryApprovalsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // APPROVAL DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendingDeliveries =
                await _context.Deliveries
                    .Include(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                    .Where(x => x.Status == "Pending")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

            var history =
                await _context.Deliveries
                    .Include(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                    .Where(x =>
                        x.Status == "Approved" ||
                        x.Status == "Rejected" ||
                        x.Status == "Delivered" ||
                        x.Status == "Cancelled")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

            ViewBag.PendingDeliveries =
                pendingDeliveries;

            return View(history);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var delivery =
                await _context.Deliveries

                    .Include(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                            .ThenInclude(x => x.Packing)
                                .ThenInclude(x => x.SalesOrder)

                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (delivery == null)
                return NotFound();

            return View(delivery);
        }

        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var delivery =
                await _context.Deliveries
                    .Include(x => x.Dispatch)
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (delivery == null)
                return NotFound();

            // Only Pending records can be approved
            if (delivery.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only Pending deliveries can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Related dispatch validation
            if (delivery.Dispatch == null)
            {
                TempData["ErrorMessage"] =
                    "The related dispatch could not be found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (delivery.Dispatch.Status != "In Transit")
            {
                TempData["ErrorMessage"] =
                    "The related dispatch is not In Transit.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // -----------------------------------------------------
            // ADMIN APPROVAL
            // -----------------------------------------------------

            delivery.Status =
                "Approved";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Delivery {delivery.DeliveryNumber} approved successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
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
            var delivery =
                await _context.Deliveries
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (delivery == null)
                return NotFound();

            if (delivery.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only Pending deliveries can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            delivery.Status =
                "Rejected";

            // Keep rejection reason inside Remarks
            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                delivery.Remarks =
                    $"Admin Rejection Reason: {rejectionReason.Trim()}" +
                    (string.IsNullOrWhiteSpace(delivery.Remarks)
                        ? ""
                        : Environment.NewLine +
                          delivery.Remarks);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Delivery {delivery.DeliveryNumber} rejected.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}