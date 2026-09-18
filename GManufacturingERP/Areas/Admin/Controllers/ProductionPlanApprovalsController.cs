using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductionPlanApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionPlanApprovalsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PENDING + HISTORY
        // =========================================================
        public async Task<IActionResult> Index()
        {
            // Pending Production Plans
            var pendingPlans = await _context.ProductionPlans
                .Include(x => x.SalesOrder)
                .Where(x => x.ProductionStatus == "PendingApproval")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // Approval History
            // Approved = Planned
            // Rejected = Rejected
            var historyPlans = await _context.ProductionPlans
                .Include(x => x.SalesOrder)
                .Where(x =>
                    x.ProductionStatus == "Planned" ||
                    x.ProductionStatus == "Rejected" ||
                    x.ProductionStatus == "InProgress" ||
                    x.ProductionStatus == "Completed")
                .OrderByDescending(x =>
                    x.UpdatedAt ?? x.CreatedAt)
                .ToListAsync();

            ViewBag.PendingPlans = pendingPlans;
            ViewBag.HistoryPlans = historyPlans;

            return View();
        }

        // =========================================================
        // DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _context.ProductionPlans
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // =========================================================
        // APPROVE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var plan = await _context.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.ProductionStatus != "PendingApproval")
            {
                TempData["ErrorMessage"] =
                    "This production plan has already been processed.";

                return RedirectToAction(nameof(Index));
            }

            // PendingApproval → Planned
            plan.ProductionStatus = "Planned";

            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Production plan {plan.PlanNumber} approved successfully.";

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
            var plan = await _context.ProductionPlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.ProductionStatus != "PendingApproval")
            {
                TempData["ErrorMessage"] =
                    "This production plan has already been processed.";

                return RedirectToAction(nameof(Index));
            }

            plan.ProductionStatus = "Rejected";

            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                plan.ProductionRemarks =
                    $"Rejected by Admin: {rejectionReason}";
            }

            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Production plan {plan.PlanNumber} rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}