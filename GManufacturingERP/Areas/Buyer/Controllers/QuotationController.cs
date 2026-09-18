
using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class QuotationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuotationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX - MY QUOTATIONS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var buyerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            var quotations = await _context.Quotations
                .Where(x => x.BuyerId == buyerId)
                .Include(x => x.SalesOrder)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(quotations);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var buyerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            var quotation = await _context.Quotations
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.BuyerId == buyerId);

            if (quotation == null)
            {
                return NotFound();
            }

            return View(quotation);
        }

        // =====================================================
        // CREATE QUOTATION REQUEST - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var buyerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == buyerId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new CreateQuotationRequestViewModel
            {
                Unit = "Pieces"
            };

            return View(model);
        }

        // =====================================================
        // CREATE QUOTATION REQUEST - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateQuotationRequestViewModel model)
        {
            var buyerId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var buyer = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == buyerId);

            if (buyer == null)
            {
                return NotFound();
            }

            var now = DateTime.UtcNow;

            var quotation = new Quotation
            {
                QuotationNumber =
                    $"QT-{now:yyyyMMddHHmmssfff}",

                BuyerId = buyerId,

                CompanyName =
                    buyer.CompanyName ?? "N/A",

                ContactPerson =
                    buyer.FullName
                    ?? buyer.UserName
                    ?? "N/A",

                BuyerEmail = buyer.Email,

                BuyerPhone = buyer.PhoneNumber,

                ProductDescription =
                    model.ProductDescription.Trim(),

                Quantity = model.Quantity,

                Unit =
                    string.IsNullOrWhiteSpace(model.Unit)
                        ? "Pieces"
                        : model.Unit.Trim(),

                RequiredDeliveryDate =
                    model.RequiredDeliveryDate,

                ShippingCountry =
                    model.ShippingCountry,

                ShippingAddress =
                    model.ShippingAddress,

                QuotationDate =
                    DateTime.Today,

                ValidUntil =
                    DateTime.Today.AddDays(30),

                Status = "Requested",

                RequestedBy =
                    buyer.UserName
                    ?? buyer.Email,

                Remarks = model.Remarks,

                CreatedAt = now
            };

            _context.Quotations.Add(quotation);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Quotation request {quotation.QuotationNumber} submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ACCEPT QUOTATION
        // Status: Sent -> AcceptedPendingPayment
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var buyerId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            // Ensure quotation belongs to the logged-in buyer
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.BuyerId == buyerId);

            if (quotation == null)
            {
                return NotFound();
            }

            // Only sent quotations can be accepted
            if (quotation.Status != "Sent")
            {
                TempData["Error"] =
                    "Only sent quotations can be accepted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Check quotation expiry
            if (quotation.ValidUntil.Date < DateTime.Today)
            {
                TempData["Error"] =
                    "This quotation has expired.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Validate advance payment terms
            if (quotation.AdvancePaymentPercentage <= 0 ||
                quotation.AdvancePaymentAmount <= 0)
            {
                TempData["Error"] =
                    "Advance payment terms are not configured. Please contact Sales.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Accept quotation, but wait for advance payment
            quotation.Status =
                "AcceptedPendingPayment";

            quotation.AcceptedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quotation accepted. Please proceed with advance payment.";

            // Redirect directly to Advance Payment page
            return RedirectToAction(
                "Advance",
                "Payment",
                new
                {
                    area = "Buyer",
                    quotationId = quotation.Id
                });
        }

        // =====================================================
        // REJECT QUOTATION
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var buyerId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(buyerId))
            {
                return Unauthorized();
            }

            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.BuyerId == buyerId);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status != "Sent")
            {
                TempData["Error"] =
                    "Only sent quotations can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            quotation.Status = "Rejected";

            quotation.RejectedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quotation rejected.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}