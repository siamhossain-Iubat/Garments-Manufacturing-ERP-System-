
using System.Globalization;
using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.Services;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SSLCommerzService _sslCommerz;
        private readonly SSLCommerzSettings _settings;

        public PaymentController(
            ApplicationDbContext context,
            SSLCommerzService sslCommerz,
            SSLCommerzSettings settings)
        {
            _context = context;
            _sslCommerz = sslCommerz;
            _settings = settings;
        }

        // =========================================
        // ADVANCE PAYMENT PAGE
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Advance(int quotationId)
        {
            var buyerId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(buyerId))
                return Unauthorized();

            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(q =>
                    q.Id == quotationId &&
                    q.BuyerId == buyerId);

            if (quotation == null)
                return NotFound();

            if (quotation.Status != "AcceptedPendingPayment")
            {
                TempData["Error"] =
                    "This quotation is not awaiting payment.";

                return RedirectToAction(
                    "Details",
                    "Quotation",
                    new
                    {
                        area = "Buyer",
                        id = quotationId
                    });
            }

            var model = new AdvancePaymentViewModel
            {
                QuotationId = quotation.Id,
                QuotationNumber = quotation.QuotationNumber,
                QuotationTotal = quotation.GrandTotal,

                AdvancePaymentPercentage =
                    quotation.AdvancePaymentPercentage,

                AdvancePaymentAmount =
                    quotation.AdvancePaymentAmount,

                BuyerName = quotation.ContactPerson,
                BuyerEmail = quotation.BuyerEmail,
                BuyerPhone = quotation.BuyerPhone,
                ShippingAddress = quotation.ShippingAddress
            };

            return View(model);
        }


        // =========================================
        // INITIATE SSLCommerz PAYMENT
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initiate(int quotationId)
        {
            var buyerId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(buyerId))
                return Unauthorized();

            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(q =>
                    q.Id == quotationId &&
                    q.BuyerId == buyerId);

            if (quotation == null)
                return NotFound();

            if (quotation.Status != "AcceptedPendingPayment")
            {
                TempData["Error"] =
                    "Quotation is not awaiting payment.";

                return RedirectToAction(
                    nameof(Advance),
                    new { quotationId });
            }

            if (quotation.AdvancePaymentAmount <= 0)
            {
                TempData["Error"] =
                    "Invalid advance payment amount.";

                return RedirectToAction(
                    nameof(Advance),
                    new { quotationId });
            }

            // Prevent creating another pending payment
            var existingPending =
                await _context.AdvancePayments
                    .AnyAsync(p =>
                        p.QuotationId == quotationId &&
                        p.BuyerId == buyerId &&
                        p.Status == "Pending");

            if (existingPending)
            {
                TempData["Error"] =
                    "A pending payment already exists. " +
                    "Please wait or contact support.";

                return RedirectToAction(
                    nameof(Advance),
                    new { quotationId });
            }

            var transactionId =
                _sslCommerz.GenerateTransactionId();

            var payment = new AdvancePayment
            {
                QuotationId = quotation.Id,
                BuyerId = buyerId,
                Amount = quotation.AdvancePaymentAmount,
                Currency = "BDT",
                TransactionId = transactionId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.AdvancePayments.Add(payment);

            await _context.SaveChangesAsync();

            var baseUrl = _settings.ReturnBaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "ReturnBaseUrl is not configured.";

                await _context.SaveChangesAsync();

                return BadRequest(
                    "Payment return URL is not configured.");
            }

            var parameters =
                new Dictionary<string, string>
                {
                    ["store_id"] = _settings.StoreId,
                    ["store_passwd"] = _settings.StorePassword,

                    ["total_amount"] =
                        payment.Amount.ToString(
                            "0.00",
                            CultureInfo.InvariantCulture),

                    ["currency"] = "BDT",
                    ["tran_id"] = transactionId,

                    ["success_url"] =
                        $"{baseUrl}/Buyer/Payment/Success",

                    ["fail_url"] =
                        $"{baseUrl}/Buyer/Payment/Fail",

                    ["cancel_url"] =
                        $"{baseUrl}/Buyer/Payment/Cancel",

                    ["cus_name"] = quotation.ContactPerson,
                    ["cus_email"] = quotation.BuyerEmail,
                    ["cus_phone"] = quotation.BuyerPhone,

                    ["cus_add1"] =
                        string.IsNullOrWhiteSpace(
                            quotation.ShippingAddress)
                            ? "N/A"
                            : quotation.ShippingAddress,

                    ["cus_city"] = "N/A",
                    ["cus_country"] = "Bangladesh",

                    ["shipping_method"] = "NO",

                    ["product_name"] =
                        $"Advance payment - {quotation.QuotationNumber}",

                    ["product_category"] = "Manufacturing",
                    ["product_profile"] = "general"
                };

            try
            {
                var response =
                    await _sslCommerz.InitiatePaymentAsync(
                        parameters);

                if (response.Status == "SUCCESS" &&
                    !string.IsNullOrWhiteSpace(
                        response.GatewayPageURL))
                {
                    return Redirect(
                        response.GatewayPageURL);
                }

                payment.Status = "Failed";

                payment.Remarks =
                    response.FailedReason;

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Could not start SSLCommerz payment.";

                return RedirectToAction(
                    nameof(Advance),
                    new { quotationId });
            }
            catch
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Payment gateway initiation failed.";

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Payment gateway error. Please try again.";

                return RedirectToAction(
                    nameof(Advance),
                    new { quotationId });
            }
        }


        // =========================================
        // SUCCESS CALLBACK
        // =========================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Success()
        {
            var form = await Request.ReadFormAsync();

            var transactionId =
                form["tran_id"].FirstOrDefault();

            var validationId =
                form["val_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(transactionId) ||
                string.IsNullOrWhiteSpace(validationId))
            {
                return BadRequest(
                    "Missing transaction information.");
            }

            // =========================================
            // LOAD PAYMENT
            // =========================================

            var payment = await _context.AdvancePayments
                .Include(p => p.Quotation)
                .FirstOrDefaultAsync(p =>
                    p.TransactionId == transactionId);

            if (payment == null)
                return NotFound();

            // =========================================
            // IDEMPOTENCY CHECK
            // =========================================

            if (payment.Status == "Paid")
            {
                return RedirectToAction(
                    nameof(Result),
                    new { id = payment.Id });
            }

            // =========================================
            // SERVER-SIDE SSLCommerz VALIDATION
            // =========================================

            var validation =
                await _sslCommerz.ValidatePaymentAsync(
                    validationId);

            if (validation == null)
            {
                // Validation unavailable is not proof of payment failure.
                // Keep Pending so it can be checked/reconciled later.
                payment.Remarks =
                    "Gateway validation unavailable. " +
                    "Manual reconciliation required.";

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Result),
                    new { id = payment.Id });
            }

            var validStatus =
                string.Equals(
                    validation.Status,
                    "VALID",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    validation.Status,
                    "VALIDATED",
                    StringComparison.OrdinalIgnoreCase);

            var amountMatches =
                decimal.TryParse(
                    validation.Amount,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var validatedAmount) &&
                validatedAmount == payment.Amount;

            var transactionMatches =
                string.Equals(
                    validation.TranId,
                    payment.TransactionId,
                    StringComparison.Ordinal);

            var currencyMatches =
                string.Equals(
                    validation.Currency,
                    payment.Currency,
                    StringComparison.OrdinalIgnoreCase);

            if (!validStatus ||
                !amountMatches ||
                !transactionMatches ||
                !currencyMatches)
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Gateway validation mismatch.";

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Result),
                    new { id = payment.Id });
            }

            // =========================================
            // PROCESS VERIFIED PAYMENT + SALES ORDER
            // =========================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Reload records inside the transaction
                var verifiedPayment =
                    await _context.AdvancePayments
                        .Include(p => p.Quotation)
                        .FirstOrDefaultAsync(p =>
                            p.TransactionId == transactionId);

                if (verifiedPayment == null)
                    return NotFound();

                var quotation = verifiedPayment.Quotation;

                if (quotation == null)
                    return NotFound();

                // =========================================
                // DUPLICATE CALLBACK PROTECTION
                // =========================================

                if (verifiedPayment.Status == "Paid" ||
                    quotation.SalesOrderId.HasValue)
                {
                    await transaction.CommitAsync();

                    return RedirectToAction(
                        nameof(Result),
                        new { id = verifiedPayment.Id });
                }

                // =========================================
                // VERIFY PAYMENT / QUOTATION RELATION
                // =========================================

                if (verifiedPayment.QuotationId != quotation.Id)
                    return BadRequest("Quotation mismatch.");

                if (verifiedPayment.BuyerId != quotation.BuyerId)
                    return BadRequest("Buyer mismatch.");

                if (verifiedPayment.Amount !=
                    quotation.AdvancePaymentAmount)
                {
                    return BadRequest(
                        "Payment amount mismatch.");
                }

                if (quotation.Status != "AcceptedPendingPayment")
                {
                    return BadRequest(
                        "Quotation is not awaiting advance payment.");
                }

                if (quotation.GrandTotal <= 0 ||
                    quotation.AdvancePaymentAmount <= 0)
                {
                    return BadRequest(
                        "Invalid quotation amount.");
                }

                // =========================================
                // CREATE SALES ORDER
                // =========================================

                var now = DateTime.UtcNow;

                var salesOrder = new SalesOrder
                {
                    // Order information
                    OrderNumber =
                        $"SO-{now:yyyyMMddHHmmssfff}",

                    BuyerId = quotation.BuyerId,
                    CompanyName = quotation.CompanyName,
                    ContactPerson = quotation.ContactPerson,
                    BuyerEmail = quotation.BuyerEmail,
                    BuyerPhone = quotation.BuyerPhone,

                    // Product information
                    ProductDescription =
                        quotation.ProductDescription,

                    Quantity = quotation.Quantity,
                    Unit = quotation.Unit,

                    // Delivery information
                    RequiredDeliveryDate =
                        quotation.RequiredDeliveryDate,

                    ShippingCountry =
                        quotation.ShippingCountry,

                    ShippingAddress =
                        quotation.ShippingAddress,

                    // Commercial information
                    UnitPrice = quotation.UnitPrice,
                    Subtotal = quotation.Subtotal,
                    Discount = quotation.Discount,
                    Tax = quotation.Tax,
                    GrandTotal = quotation.GrandTotal,

                    // Other information
                    SpecialInstructions =
                        quotation.TermsAndConditions,

                    OrderStatus = "Pending",

                    SalesRemarks =
                        $"Created from quotation " +
                        $"{quotation.QuotationNumber}. " +
                        $"Advance payment received: " +
                        $"{verifiedPayment.Amount:0.00} BDT.",

                    CreatedAt = now
                };

                _context.SalesOrders.Add(salesOrder);

                // =========================================
                // MARK ADVANCE PAYMENT AS PAID
                // =========================================

                verifiedPayment.Status = "Paid";

                verifiedPayment.ValidationId =
                    validation.ValId;

                verifiedPayment.BankTransactionId =
                    validation.BankTranId;

                verifiedPayment.PaidAt = now;

                // =========================================
                // LINK QUOTATION TO SALES ORDER
                // =========================================

                quotation.SalesOrder = salesOrder;
                quotation.Status = "Converted";

                // One SaveChanges inside one DB transaction
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(
                    nameof(Result),
                    new { id = verifiedPayment.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =========================================
        // FAIL CALLBACK
        // =========================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Fail()
        {
            var form = await Request.ReadFormAsync();

            var transactionId =
                form["tran_id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var payment =
                    await _context.AdvancePayments
                        .FirstOrDefaultAsync(p =>
                            p.TransactionId == transactionId);

                if (payment != null &&
                    payment.Status == "Pending")
                {
                    payment.Status = "Failed";

                    await _context.SaveChangesAsync();
                }
            }

            return View("PaymentFailed");
        }


        // =========================================
        // CANCEL CALLBACK
        // =========================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Cancel()
        {
            var form = await Request.ReadFormAsync();

            var transactionId =
                form["tran_id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var payment =
                    await _context.AdvancePayments
                        .FirstOrDefaultAsync(p =>
                            p.TransactionId == transactionId);

                if (payment != null &&
                    payment.Status == "Pending")
                {
                    payment.Status = "Cancelled";

                    await _context.SaveChangesAsync();
                }
            }

            return View("PaymentCancelled");
        }


        // =========================================
        // PAYMENT RESULT
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var buyerId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var payment =
                await _context.AdvancePayments
                    .Include(p => p.Quotation)
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.BuyerId == buyerId);

            if (payment == null)
                return NotFound();

            return View(payment);
        }
    }
}