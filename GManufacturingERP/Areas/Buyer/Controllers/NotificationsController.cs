using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // NOTIFICATION LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var notifications =
                await _context.Notifications

                    .Include(x =>
                        x.PartialShipmentRequest)

                    .Where(x =>
                        x.RecipientUserId == user.Id)

                    .OrderByDescending(
                        x => x.CreatedAt)

                    .ToListAsync();

            return View(notifications);
        }

        // =========================================================
        // MARK NOTIFICATION AS READ
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            int id)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.RecipientUserId == user.Id);

            if (notification != null)
            {
                notification.IsRead = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // BUYER APPROVE / DECLINE PARTIAL SHIPMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PartialShipmentResponse(
            int requestId,
            bool approve,
            string? remarks)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            // -----------------------------------------------------
            // Find request belonging to current buyer
            // -----------------------------------------------------

            var request =
                await _context.PartialShipmentRequests

                    .Include(x =>
                        x.SalesOrder)

                    .Include(x =>
                        x.FinishedGoodsStock)

                    .FirstOrDefaultAsync(x =>
                        x.Id == requestId &&

                        x.BuyerId == user.Id &&

                        x.Status == "Pending");

            if (request == null)
            {
                TempData["ErrorMessage"] =
                    "This partial shipment request is no longer available.";

                return RedirectToAction(
                    nameof(Index));
            }

            // -----------------------------------------------------
            // Update request status
            // -----------------------------------------------------

            request.Status =
                approve
                    ? "Approved"
                    : "Declined";

            request.RespondedAt =
                DateTime.UtcNow;

            request.BuyerRemarks =
                string.IsNullOrWhiteSpace(remarks)
                    ? null
                    : remarks.Trim();

            // -----------------------------------------------------
            // Mark related notification as read
            // -----------------------------------------------------

            var relatedNotification =
                await _context.Notifications
                    .FirstOrDefaultAsync(x =>
                        x.RecipientUserId == user.Id &&

                        x.PartialShipmentRequestId ==
                            request.Id);

            if (relatedNotification != null)
            {
                relatedNotification.IsRead = true;
            }

            // -----------------------------------------------------
            // Save response
            // -----------------------------------------------------

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // Buyer message
            // -----------------------------------------------------

            if (approve)
            {
                TempData["SuccessMessage"] =
                    "Partial shipment approved successfully. " +
                    "Finance can now proceed with packing.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Partial shipment declined. " +
                    "Packing will wait for additional finished goods.";
            }

            return RedirectToAction(
                nameof(Index));
        }
    }
}