using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BuyerApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuyerApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PENDING BUYERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var buyers = await _context.Users
                .Where(x =>
                    x.UserType == "External" &&
                    x.AccountStatus == "PendingApproval")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(buyers);
        }

        // =====================================================
        // ALL BUYERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AllBuyers()
        {
            var buyers = await _context.Users
                .Where(x => x.UserType == "External")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(buyers);
        }

        // =====================================================
        // BUYER DETAILS
        // =====================================================

        [HttpGet]
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

        // =====================================================
        // APPROVE BUYER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Invalid buyer ID.";
                return RedirectToAction(nameof(Index));
            }

            var buyer = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserType == "External");

            if (buyer == null)
            {
                TempData["ErrorMessage"] = "Buyer account not found.";
                return RedirectToAction(nameof(Index));
            }

            buyer.AccountStatus = "Approved";
            buyer.IsActive = true;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Buyer account approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Approval failed: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REJECT BUYER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            string id,
            string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Invalid buyer ID.";
                return RedirectToAction(nameof(Index));
            }

            var buyer = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserType == "External");

            if (buyer == null)
            {
                TempData["ErrorMessage"] = "Buyer account not found.";
                return RedirectToAction(nameof(Index));
            }

            buyer.AccountStatus = "Rejected";
            buyer.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Buyer account rejected successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Rejection failed: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}