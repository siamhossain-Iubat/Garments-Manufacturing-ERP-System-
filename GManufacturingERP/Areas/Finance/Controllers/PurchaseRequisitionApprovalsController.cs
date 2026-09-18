using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager")]
    public class PurchaseRequisitionApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchaseRequisitionApprovalsController(
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
            var pendingRequisitions =
                await _context.PurchaseRequisitions
                    .Include(x => x.MaterialRequirementPlan)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.RequisitionStatus == "Submitted")
                    .OrderByDescending(x =>
                        x.SubmittedAt ?? x.CreatedAt)
                    .ToListAsync();

            var historyRequisitions =
                await _context.PurchaseRequisitions
                    .Include(x => x.MaterialRequirementPlan)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.RequisitionStatus == "Approved" ||
                        x.RequisitionStatus == "Rejected")
                    .OrderByDescending(x =>
                        x.ApprovedAt ?? x.SubmittedAt ?? x.CreatedAt)
                    .ToListAsync();

            ViewBag.PendingRequisitions =
                pendingRequisitions;

            ViewBag.HistoryRequisitions =
                historyRequisitions;

            return View();
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var requisition =
                await _context.PurchaseRequisitions
                    .Include(x => x.MaterialRequirementPlan)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }

            return View(requisition);
        }

        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var requisition =
                await _context.PurchaseRequisitions
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }

            if (requisition.RequisitionStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted requisitions can be approved.";

                return RedirectToAction(
                    nameof(Index));
            }

            requisition.RequisitionStatus =
                "Approved";

            requisition.ApprovedBy =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.Identity?.Name
                ?? "Finance & Logistics Manager";

            requisition.ApprovedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Purchase Requisition {requisition.RequisitionNumber} approved successfully.";

            return RedirectToAction(
                nameof(Index));
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
            var requisition =
                await _context.PurchaseRequisitions
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }

            if (requisition.RequisitionStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted requisitions can be rejected.";

                return RedirectToAction(
                    nameof(Index));
            }

            requisition.RequisitionStatus =
                "Rejected";

            requisition.RejectionReason =
                string.IsNullOrWhiteSpace(rejectionReason)
                    ? "Rejected by Finance & Logistics Manager."
                    : rejectionReason;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Purchase Requisition {requisition.RequisitionNumber} rejected.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}