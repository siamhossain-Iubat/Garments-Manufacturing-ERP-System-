using System.Security.Claims;
using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MaterialIssueApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaterialIssueApprovalsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: Admin/MaterialIssueApprovals
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Pending Material Issues
            var pendingIssues =
                await _context.MaterialIssues
                    .Include(x => x.Items)
                    .Include(x => x.MaterialRequirementPlan)
                    .Where(x => x.Status == "Submitted")
                    .OrderByDescending(x => x.IssueDate)
                    .ToListAsync();


            // Approval History
            var history =
                await _context.MaterialIssues
                    .Include(x => x.Items)
                    .Include(x => x.MaterialRequirementPlan)
                    .Where(x =>
                        x.Status == "Approved" ||
                        x.Status == "Rejected")
                    .OrderByDescending(x => x.IssueDate)
                    .ToListAsync();


            ViewBag.PendingIssues = pendingIssues;
            ViewBag.History = history;


            return View();
        }


        // GET: Admin/MaterialIssueApprovals/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var issue =
                await _context.MaterialIssues
                    .Include(x => x.Items)
                    .Include(x => x.MaterialRequirementPlan)
                        .ThenInclude(x => x!.ProductionPlan)
                    .FirstOrDefaultAsync(x => x.Id == id);


            if (issue == null)
            {
                return NotFound();
            }


            return View(issue);
        }


        // POST: Admin/MaterialIssueApprovals/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var issue =
                await _context.MaterialIssues
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == id);


            if (issue == null)
            {
                return NotFound();
            }


            if (issue.Status != "Submitted")
            {
                TempData["Error"] =
                    "Only submitted material issues can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            var approvedBy =
                User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? "Admin";


            // Validate ALL stock before changing anything.
            foreach (var item in issue.Items)
            {
                var stock =
                    await _context.InventoryStocks
                        .FirstOrDefaultAsync(x =>
                            x.Id == item.InventoryStockId);


                if (stock == null)
                {
                    TempData["Error"] =
                        $"Inventory stock not found for {item.MaterialName}.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }


                if (item.RequestedQuantity <= 0)
                {
                    TempData["Error"] =
                        $"Invalid issue quantity for {item.MaterialName}.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }


                if (item.RequestedQuantity >
                    stock.AvailableQuantity)
                {
                    TempData["Error"] =
                        $"Insufficient stock for {stock.MaterialName}. " +
                        $"Available: {stock.AvailableQuantity} {stock.Unit}.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }
            }


            // Now perform inventory deduction.
            foreach (var item in issue.Items)
            {
                var stock =
                    await _context.InventoryStocks
                        .FirstAsync(x =>
                            x.Id == item.InventoryStockId);


                stock.AvailableQuantity -=
                    item.RequestedQuantity;

                stock.UpdatedAt =
                    DateTime.UtcNow;


                item.IssuedQuantity =
                    item.RequestedQuantity;


                var transaction =
                    new GManufacturingERP.Models.Entities
                        .InventoryTransaction
                    {
                        InventoryStockId =
                            stock.Id,

                        TransactionType =
                            "MATERIAL_ISSUE",

                        ReferenceNumber =
                            issue.IssueNumber,

                        QuantityIn = 0,

                        QuantityOut =
                            item.RequestedQuantity,

                        BalanceAfter =
                            stock.AvailableQuantity,

                        TransactionDate =
                            DateTime.UtcNow,

                        PerformedBy =
                            approvedBy,

                        Remarks =
                            $"Material issued to production " +
                            $"{issue.ProductionReference}"
                    };


                _context.InventoryTransactions
                    .Add(transaction);
            }


            // Update Material Issue status.
            issue.Status = "Approved";

            issue.ApprovedBy =
                approvedBy;

            issue.ApprovedAt =
                DateTime.UtcNow;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Material issue approved successfully. " +
                "Inventory stock has been updated.";


            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // POST: Admin/MaterialIssueApprovals/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? rejectionReason)
        {
            var issue =
                await _context.MaterialIssues
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (issue == null)
            {
                return NotFound();
            }


            if (issue.Status != "Submitted")
            {
                TempData["Error"] =
                    "Only submitted material issues can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            issue.Status = "Rejected";


            if (!string.IsNullOrWhiteSpace(
                rejectionReason))
            {
                issue.Remarks =
                    $"Rejected by Admin: " +
                    $"{rejectionReason}\n\n" +
                    issue.Remarks;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Material issue rejected successfully.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}