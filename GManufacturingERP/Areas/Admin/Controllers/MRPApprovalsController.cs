using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MRPApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MRPApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PENDING + HISTORY
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // MRP waiting for Admin approval
            var pendingMRPs = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .Where(x => x.MRPStatus == "Submitted")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // Previously processed MRPs
            var historyMRPs = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .Where(x =>
                    x.MRPStatus == "Approved" ||
                    x.MRPStatus == "Rejected")
                .OrderByDescending(x =>
                    x.UpdatedAt ?? x.CreatedAt)
                .ToListAsync();

            ViewBag.PendingMRPs = pendingMRPs;
            ViewBag.HistoryMRPs = historyMRPs;

            return View();
        }

        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mrp == null)
            {
                return NotFound();
            }

            return View(mrp);
        }

        // =====================================================
        // APPROVE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var mrp = await _context.MaterialRequirementPlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mrp == null)
            {
                return NotFound();
            }

            // Only Submitted MRP can be approved
            if (mrp.MRPStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "This MRP has already been processed.";

                return RedirectToAction(nameof(Index));
            }

            mrp.MRPStatus = "Approved";
            mrp.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"MRP {mrp.MRPNumber} approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REJECT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? rejectionReason)
        {
            var mrp = await _context.MaterialRequirementPlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mrp == null)
            {
                return NotFound();
            }

            // Only Submitted MRP can be rejected
            if (mrp.MRPStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "This MRP has already been processed.";

                return RedirectToAction(nameof(Index));
            }

            mrp.MRPStatus = "Rejected";

            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                mrp.Remarks =
                    $"Rejected by Admin: {rejectionReason}";
            }

            mrp.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"MRP {mrp.MRPNumber} rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}