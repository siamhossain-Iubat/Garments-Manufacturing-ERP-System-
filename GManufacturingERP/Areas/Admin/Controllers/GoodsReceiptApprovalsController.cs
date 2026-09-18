using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class GoodsReceiptApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoodsReceiptApprovalsController(
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
            var pendingReceipts =
                await _context.GoodsReceipts
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.ReceiptStatus == "Submitted")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();


            var historyReceipts =
                await _context.GoodsReceipts
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.Items)
                    .Where(x =>
                        x.ReceiptStatus == "Approved" ||
                        x.ReceiptStatus == "Rejected")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();


            ViewBag.PendingReceipts =
                pendingReceipts;

            ViewBag.HistoryReceipts =
                historyReceipts;


            return View();
        }


        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var receipt =
                await _context.GoodsReceipts
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                return NotFound();
            }


            return View(receipt);
        }


        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var receipt =
                await _context.GoodsReceipts
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                return NotFound();
            }


            if (receipt.ReceiptStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted GRN can be approved.";

                return RedirectToAction(
                    nameof(Index));
            }


            receipt.ReceiptStatus =
                "Approved";


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"Goods Receipt {receipt.GRNNumber} approved successfully. Finance can now sync inventory.";


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
            var receipt =
                await _context.GoodsReceipts
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                return NotFound();
            }


            if (receipt.ReceiptStatus != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "Only Submitted GRN can be rejected.";

                return RedirectToAction(
                    nameof(Index));
            }


            receipt.ReceiptStatus =
                "Rejected";


            if (!string.IsNullOrWhiteSpace(
                rejectionReason))
            {
                receipt.Remarks =
                    $"Rejected by Admin: {rejectionReason}";
            }


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"Goods Receipt {receipt.GRNNumber} rejected.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}