
using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "SalesBuyerManager,Admin")]
    public class QuotationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuotationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX - ALL QUOTATIONS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var quotations = await _context.Quotations
                .Include(x => x.Buyer)
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
            var quotation = await _context.Quotations
                .Include(x => x.Buyer)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            return View(quotation);
        }

        // =====================================================
        // REVIEW REQUEST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id)
        {
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status != "Requested")
            {
                TempData["Error"] =
                    "Only requested quotations can be reviewed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            quotation.Status = "Under Review";
            quotation.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quotation request moved to Under Review.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =====================================================
        // PREPARE QUOTATION - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Prepare(int id)
        {
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status != "Requested" &&
                quotation.Status != "Under Review")
            {
                TempData["Error"] =
                    "This quotation cannot be prepared.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var model = new PrepareQuotationViewModel
            {
                Id = quotation.Id,

                QuotationNumber =
                    quotation.QuotationNumber,

                CompanyName =
                    quotation.CompanyName,

                ContactPerson =
                    quotation.ContactPerson,

                BuyerEmail =
                    quotation.BuyerEmail,

                BuyerPhone =
                    quotation.BuyerPhone,

                ProductDescription =
                    quotation.ProductDescription,

                Quantity =
                    quotation.Quantity,

                Unit =
                    quotation.Unit,

                RequiredDeliveryDate =
                    quotation.RequiredDeliveryDate,

                ShippingCountry =
                    quotation.ShippingCountry,

                ShippingAddress =
                    quotation.ShippingAddress,

                UnitPrice =
                    quotation.UnitPrice,

                Discount =
                    quotation.Discount,

                Tax =
                    quotation.Tax,

                // ADVANCE PAYMENT
                AdvancePaymentPercentage =
                    quotation.AdvancePaymentPercentage,

                AdvancePaymentAmount =
                    quotation.AdvancePaymentAmount,

                ValidUntil =
                    quotation.ValidUntil,

                TermsAndConditions =
                    quotation.TermsAndConditions,

                Remarks =
                    quotation.Remarks
            };

            // =====================================================
            // CALCULATE QUOTATION TOTAL
            // =====================================================

            model.Subtotal =
                model.Quantity * model.UnitPrice;

            model.GrandTotal =
                model.Subtotal -
                model.Discount +
                model.Tax;

            // =====================================================
            // ADVANCE PAYMENT CALCULATION
            // =====================================================

            model.AdvancePaymentAmount =
                Math.Round(
                    model.GrandTotal
                    * model.AdvancePaymentPercentage
                    / 100m,
                    2
                );

            return View(model);
        }

        // =====================================================
        // PREPARE QUOTATION - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prepare(
            PrepareQuotationViewModel model)
        {
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status != "Requested" &&
                quotation.Status != "Under Review")
            {
                TempData["Error"] =
                    "This quotation cannot be prepared.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.Id });
            }

            // =====================================================
            // SERVER SIDE CALCULATION
            // =====================================================

            model.Subtotal =
                model.Quantity * model.UnitPrice;

            // =====================================================
            // DISCOUNT VALIDATION
            // =====================================================

            if (model.Discount < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Discount),
                    "Discount cannot be negative.");
            }

            if (model.Discount > model.Subtotal)
            {
                ModelState.AddModelError(
                    nameof(model.Discount),
                    "Discount cannot exceed subtotal.");
            }

            // =====================================================
            // TAX VALIDATION
            // =====================================================

            if (model.Tax < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Tax),
                    "Tax cannot be negative.");
            }

            // =====================================================
            // ADVANCE PAYMENT VALIDATION
            // =====================================================

            if (model.AdvancePaymentPercentage < 0 ||
                model.AdvancePaymentPercentage > 100)
            {
                ModelState.AddModelError(
                    nameof(model.AdvancePaymentPercentage),
                    "Advance payment percentage must be between 0 and 100.");
            }

            // =====================================================
            // GRAND TOTAL
            // =====================================================

            var taxableAmount =
                model.Subtotal - model.Discount;

            model.GrandTotal =
                taxableAmount + model.Tax;

            // =====================================================
            // ADVANCE PAYMENT CALCULATION
            // =====================================================

            model.AdvancePaymentAmount =
                Math.Round(
                    model.GrandTotal
                    * model.AdvancePaymentPercentage
                    / 100m,
                    2
                );

            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // =====================================================
            // SAVE COMMERCIAL VALUES INTO QUOTATION
            // =====================================================

            quotation.Subtotal =
                quotation.Quantity * model.UnitPrice;

            quotation.UnitPrice =
                model.UnitPrice;

            quotation.Discount =
                model.Discount;

            quotation.Tax =
                model.Tax;

            quotation.GrandTotal =
                quotation.Subtotal
                - quotation.Discount
                + quotation.Tax;

            // =====================================================
            // SAVE ADVANCE PAYMENT
            // =====================================================

            quotation.AdvancePaymentPercentage =
                model.AdvancePaymentPercentage;

            quotation.AdvancePaymentAmount =
                Math.Round(
                    quotation.GrandTotal
                    * quotation.AdvancePaymentPercentage
                    / 100m,
                    2
                );

            // =====================================================
            // OTHER INFORMATION
            // =====================================================

            quotation.ValidUntil =
                model.ValidUntil;

            quotation.TermsAndConditions =
                model.TermsAndConditions;

            quotation.Remarks =
                model.Remarks;

            quotation.Status =
                "Prepared";

            quotation.PreparedAt =
                DateTime.UtcNow;

            // =====================================================
            // SAVE CHANGES
            // =====================================================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Quotation {quotation.QuotationNumber} prepared successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = quotation.Id });
        }

        // =====================================================
        // SEND QUOTATION
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status != "Prepared")
            {
                TempData["Error"] =
                    "Only prepared quotations can be sent.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (quotation.GrandTotal <= 0)
            {
                TempData["Error"] =
                    "Quotation price must be greater than zero.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (quotation.ValidUntil.Date < DateTime.Today)
            {
                TempData["Error"] =
                    "Quotation validity date has expired.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            quotation.Status =
                "Sent";

            quotation.SentAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Quotation {quotation.QuotationNumber} sent to buyer.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =====================================================
        // CANCEL QUOTATION
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            if (quotation.Status == "Accepted" ||
                quotation.Status == "Converted")
            {
                TempData["Error"] =
                    "Accepted or converted quotations cannot be cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (quotation.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Quotation is already cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            quotation.Status =
                "Cancelled";

            quotation.CancelledAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quotation cancelled successfully.";

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================================
        // CONVERT ACCEPTED QUOTATION TO SALES ORDER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToSalesOrder(int id)
        {
            // =====================================================
            // LOAD QUOTATION
            // =====================================================

            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (quotation == null)
            {
                return NotFound();
            }

            // =====================================================
            // ONLY ACCEPTED QUOTATION CAN BE CONVERTED
            // =====================================================

            if (quotation.Status != "Accepted")
            {
                TempData["Error"] =
                    "Only accepted quotations can be converted to Sales Order.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // =====================================================
            // PREVENT DUPLICATE SALES ORDER
            // =====================================================

            if (quotation.SalesOrderId.HasValue)
            {
                TempData["Error"] =
                    "This quotation has already been converted to a Sales Order.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // =====================================================
            // CREATE SALES ORDER
            // =====================================================

            var now =
                DateTime.UtcNow;

            var salesOrder =
                new SalesOrder
                {
                    // -------------------------------------------------
                    // ORDER INFORMATION
                    // -------------------------------------------------

                    OrderNumber =
                        $"SO-{now:yyyyMMddHHmmssfff}",

                    BuyerId =
                        quotation.BuyerId,

                    CompanyName =
                        quotation.CompanyName,

                    ContactPerson =
                        quotation.ContactPerson,

                    BuyerEmail =
                        quotation.BuyerEmail,

                    BuyerPhone =
                        quotation.BuyerPhone,

                    // -------------------------------------------------
                    // PRODUCT INFORMATION
                    // -------------------------------------------------

                    ProductDescription =
                        quotation.ProductDescription,

                    Quantity =
                        quotation.Quantity,

                    Unit =
                        quotation.Unit,

                    // -------------------------------------------------
                    // DELIVERY INFORMATION
                    // -------------------------------------------------

                    RequiredDeliveryDate =
                        quotation.RequiredDeliveryDate,

                    ShippingCountry =
                        quotation.ShippingCountry,

                    ShippingAddress =
                        quotation.ShippingAddress,

                    // -------------------------------------------------
                    // COMMERCIAL INFORMATION
                    // -------------------------------------------------

                    UnitPrice =
                        quotation.UnitPrice,

                    Subtotal =
                        quotation.Subtotal,

                    Discount =
                        quotation.Discount,

                    Tax =
                        quotation.Tax,

                    GrandTotal =
                        quotation.GrandTotal,

                    // -------------------------------------------------
                    // OTHER INFORMATION
                    // -------------------------------------------------

                    SpecialInstructions =
                        quotation.TermsAndConditions,

                    OrderStatus =
                        "Pending",

                    SalesRemarks =
                        $"Created from quotation {quotation.QuotationNumber}",

                    CreatedAt =
                        now
                };

            // =====================================================
            // SAVE SALES ORDER
            // =====================================================

            _context.SalesOrders.Add(
                salesOrder);

            await _context.SaveChangesAsync();

            // =====================================================
            // LINK QUOTATION WITH SALES ORDER
            // =====================================================

            quotation.SalesOrderId =
                salesOrder.Id;

            quotation.Status =
                "Converted";

            await _context.SaveChangesAsync();

            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["Success"] =
                $"Quotation converted successfully to Sales Order {salesOrder.OrderNumber}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}