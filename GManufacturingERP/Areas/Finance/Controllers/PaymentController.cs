using System.Globalization;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.Services;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "Buyer,FinanceLogisticsManager,Admin")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SSLCommerzService _sslCommerz;
        private readonly SSLCommerzSettings _settings;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            ApplicationDbContext context,
            SSLCommerzService sslCommerz,
            SSLCommerzSettings settings,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _sslCommerz = sslCommerz;
            _settings = settings;
            _userManager = userManager;
        }

        // =========================================================
        // INDEX
        // Finance/Admin -> All Payments
        // Buyer -> Own Payments
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isFinance =
                User.IsInRole("FinanceLogisticsManager") ||
                User.IsInRole("Admin");

            var query = _context.Payments
                .Include(x => x.Invoice)
                .ThenInclude(x => x.SalesOrder)
                .AsQueryable();

            if (!isFinance)
            {
                query = query.Where(x =>
                    x.Invoice.SalesOrder != null &&
                    x.Invoice.SalesOrder.BuyerId == user.Id);
            }

            var payments = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        // =========================================================
        // CREATE - GET
        // BUYER ONLY
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> Create(int invoiceId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var invoice = await _context.Invoices
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice == null)
            {
                return NotFound("Invoice not found.");
            }

            // =====================================================
            // SECURITY: Buyer can only pay own invoice
            // =====================================================

            if (invoice.SalesOrder == null)
            {
                TempData["Error"] =
                    "This invoice is not linked to a sales order.";

                return RedirectToAction(
                    "Details",
                    "Invoice",
                    new
                    {
                        area = "Buyer",
                        id = invoice.Id
                    });
            }

            if (invoice.SalesOrder.BuyerId != user.Id)
            {
                return Forbid();
            }

            // =====================================================
            // Invoice Status
            // =====================================================

            if (invoice.Status != "Issued" &&
                invoice.Status != "Partially Paid")
            {
                TempData["Error"] =
                    "This invoice is not available for payment.";

                return RedirectToAction(
                    "Details",
                    "Invoice",
                    new
                    {
                        area = "Buyer",
                        id = invoice.Id
                    });
            }

            // =====================================================
            // Due Amount
            // =====================================================

            if (invoice.DueAmount <= 0)
            {
                TempData["Error"] =
                    "There is no outstanding amount for this invoice.";

                return RedirectToAction(
                    "Details",
                    "Invoice",
                    new
                    {
                        area = "Buyer",
                        id = invoice.Id
                    });
            }

            // =====================================================
            // Payment Model
            // =====================================================

            var model = new CreatePaymentViewModel
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                BuyerName = invoice.BuyerName,
                GrandTotal = invoice.GrandTotal,
                PaidAmount = invoice.PaidAmount,
                DueAmount = invoice.DueAmount,
                Amount = invoice.DueAmount,
                Currency = "BDT",
                PaymentMethod = "SSLCOMMERZ"
            };

            return View(model);
        }

        // =========================================================
        // CREATE - POST
        // START SSLCommerz PAYMENT
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Buyer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreatePaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var invoice = await _context.Invoices
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.InvoiceId);

            if (invoice == null)
            {
                return NotFound("Invoice not found.");
            }

            // =====================================================
            // SECURITY: Buyer Ownership
            // =====================================================

            if (invoice.SalesOrder == null)
            {
                ModelState.AddModelError(
                    "",
                    "This invoice is not linked to a sales order.");

                return View(model);
            }

            if (invoice.SalesOrder.BuyerId != user.Id)
            {
                return Forbid();
            }

            // =====================================================
            // Invoice Status
            // =====================================================

            if (invoice.Status != "Issued" &&
                invoice.Status != "Partially Paid")
            {
                ModelState.AddModelError(
                    "",
                    "This invoice is not available for payment.");

                return View(model);
            }

            // =====================================================
            // Due Amount
            // =====================================================

            if (invoice.DueAmount <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "This invoice has no outstanding amount.");

                return View(model);
            }

            // =====================================================
            // IMPORTANT
            // Never trust payment amount from browser.
            // Always use actual Invoice DueAmount.
            // =====================================================

            var paymentAmount = invoice.DueAmount;

            // =====================================================
            // Prevent Duplicate Payment Session
            // =====================================================

            var existingPayment =
                await _context.Payments
                    .Where(x =>
                        x.InvoiceId == invoice.Id &&
                        (x.Status == "Pending" ||
                         x.Status == "Processing"))
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                TempData["Error"] =
                    "A payment session is already in progress for this invoice.";

                return RedirectToAction(
                    "Details",
                    "Invoice",
                    new
                    {
                        area = "Buyer",
                        id = invoice.Id
                    });
            }

            // =====================================================
            // Transaction ID
            // =====================================================

            var transactionId =
                _sslCommerz.GenerateTransactionId();

            // =====================================================
            // Create Payment Record
            // =====================================================

            var payment = new Payment
            {
                PaymentNumber =
                    $"PAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}",

                InvoiceId = invoice.Id,

                BuyerName = invoice.BuyerName,

                Amount = paymentAmount,

                Currency = "BDT",

                PaymentMethod = "SSLCOMMERZ",

                TransactionId = transactionId,

                Status = "Pending",

                PaymentDate = DateTime.UtcNow,

                CreatedAt = DateTime.UtcNow,

                Remarks = model.Remarks
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            // =====================================================
            // BASE URL
            // =====================================================

            var returnBaseUrl =
                _settings.ReturnBaseUrl?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(returnBaseUrl))
            {
                returnBaseUrl =
                    $"{Request.Scheme}://{Request.Host}";
            }

            // =====================================================
            // CALLBACK URLs
            // =====================================================

            var successUrl =
                $"{returnBaseUrl}/Finance/Payment/Success";

            var failUrl =
                $"{returnBaseUrl}/Finance/Payment/Fail";

            var cancelUrl =
                $"{returnBaseUrl}/Finance/Payment/Cancel";

            var ipnUrl =
                $"{returnBaseUrl}/Finance/Payment/IPN";

            // =====================================================
            // SSLCommerz Parameters
            // =====================================================

            var parameters =
                new Dictionary<string, string>
                {
                    ["store_id"] =
                        _settings.StoreId,

                    ["store_passwd"] =
                        _settings.StorePassword,

                    ["total_amount"] =
                        paymentAmount.ToString(
                            "0.00",
                            CultureInfo.InvariantCulture),

                    ["currency"] =
                        "BDT",

                    ["tran_id"] =
                        transactionId,

                    ["success_url"] =
                        successUrl,

                    ["fail_url"] =
                        failUrl,

                    ["cancel_url"] =
                        cancelUrl,

                    ["ipn_url"] =
                        ipnUrl,

                    ["cus_name"] =
                        invoice.BuyerName,

                    ["cus_email"] =
                        invoice.SalesOrder.BuyerEmail
                        ?? user.Email
                        ?? "customer@example.com",

                    ["cus_add1"] =
                        invoice.SalesOrder.ShippingAddress
                        ?? "Bangladesh",

                    ["cus_city"] =
                        "Dhaka",

                    ["cus_country"] =
                        "Bangladesh",

                    ["shipping_method"] =
                        "NO",

                    ["product_name"] =
                        invoice.ProductName,

                    ["product_category"] =
                        "Manufacturing",

                    ["product_profile"] =
                        "general",

                    ["value_a"] =
                        invoice.Id.ToString(),

                    ["value_b"] =
                        payment.Id.ToString(),

                    ["value_c"] =
                        user.Id
                };

            // =====================================================
            // INITIATE PAYMENT
            // =====================================================

            try
            {
                var result =
                    await _sslCommerz
                        .InitiatePaymentAsync(parameters);

                // =================================================
                // Gateway URL Check
                // =================================================

                if (string.IsNullOrWhiteSpace(
                        result.GatewayPageURL))
                {
                    payment.Status = "Failed";

                    payment.Remarks =
                        string.IsNullOrWhiteSpace(
                            result.FailedReason)
                        ? "SSLCOMMERZ gateway URL was not returned."
                        : result.FailedReason;

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "Unable to start SSLCOMMERZ payment.";

                    return RedirectToAction(
                        "Details",
                        "Invoice",
                        new
                        {
                            area = "Buyer",
                            id = invoice.Id
                        });
                }

                // =================================================
                // Payment Processing
                // =================================================

                payment.Status = "Processing";

                await _context.SaveChangesAsync();

                // =================================================
                // REDIRECT TO SSLCommerz
                // =================================================

                return Redirect(
                    result.GatewayPageURL);
            }
            catch (Exception ex)
            {
                payment.Status = "Failed";

                payment.Remarks =
                    $"Payment initiation failed: {ex.Message}";

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Payment gateway connection failed.";

                return RedirectToAction(
                    "Details",
                    "Invoice",
                    new
                    {
                        area = "Buyer",
                        id = invoice.Id
                    });
            }
        }

        // =========================================================
        // SUCCESS CALLBACK
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Success()
        {
            var form =
                await Request.ReadFormAsync();

            var tranId =
                form["tran_id"].FirstOrDefault();

            var valId =
                form["val_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(tranId))
            {
                return BadRequest(
                    "Transaction ID missing.");
            }

            var payment =
                await _context.Payments
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x =>
                        x.TransactionId == tranId);

            if (payment == null)
            {
                return NotFound(
                    "Payment not found.");
            }

            if (!string.IsNullOrWhiteSpace(valId))
            {
                payment.ValidationId = valId;
            }

            payment.Status = "Processing";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(PaymentResult),
                new
                {
                    transactionId = tranId
                });
        }

        // =========================================================
        // PAYMENT RESULT
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PaymentResult(
            string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest();
            }

            var payment =
                await _context.Payments
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x =>
                        x.TransactionId == transactionId);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // =========================================================
        // FAIL CALLBACK
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Fail()
        {
            var form =
                await Request.ReadFormAsync();

            var tranId =
                form["tran_id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment =
                    await _context.Payments
                        .FirstOrDefaultAsync(x =>
                            x.TransactionId == tranId);

                if (payment != null &&
                    payment.Status != "Completed")
                {
                    payment.Status = "Failed";

                    payment.Remarks =
                        "SSLCOMMERZ payment failed.";

                    await _context.SaveChangesAsync();
                }
            }

            return View();
        }

        // =========================================================
        // CANCEL CALLBACK
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Cancel()
        {
            var form =
                await Request.ReadFormAsync();

            var tranId =
                form["tran_id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment =
                    await _context.Payments
                        .FirstOrDefaultAsync(x =>
                            x.TransactionId == tranId);

                if (payment != null &&
                    payment.Status != "Completed")
                {
                    payment.Status = "Cancelled";

                    payment.Remarks =
                        "Payment cancelled by customer.";

                    await _context.SaveChangesAsync();
                }
            }

            return View();
        }

        // =========================================================
        // IPN
        // FINAL PAYMENT VERIFICATION
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IPN()
        {
            var form =
                await Request.ReadFormAsync();

            var tranId =
                form["tran_id"].FirstOrDefault();

            var valId =
                form["val_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(tranId) ||
                string.IsNullOrWhiteSpace(valId))
            {
                return BadRequest();
            }

            var payment =
                await _context.Payments
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x =>
                        x.TransactionId == tranId);

            if (payment == null)
            {
                return NotFound();
            }

            // Already completed
            if (payment.Status == "Completed")
            {
                return Ok();
            }

            // =====================================================
            // Validate with SSLCommerz
            // =====================================================

            var validation =
                await _sslCommerz
                    .ValidatePaymentAsync(valId);

            if (validation == null)
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Unable to validate payment.";

                await _context.SaveChangesAsync();

                return BadRequest();
            }

            // =====================================================
            // Validate Status
            // =====================================================

            if (!string.Equals(
                    validation.Status,
                    "VALID",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    validation.Status,
                    "VALIDATED",
                    StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = "Failed";

                payment.Remarks =
                    $"Invalid payment status: {validation.Status}";

                await _context.SaveChangesAsync();

                return BadRequest();
            }

            // =====================================================
            // Validate Transaction ID
            // =====================================================

            if (!string.Equals(
                    validation.TranId,
                    payment.TransactionId,
                    StringComparison.Ordinal))
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Transaction ID validation failed.";

                await _context.SaveChangesAsync();

                return BadRequest();
            }

            // =====================================================
            // Validate Amount
            // =====================================================

            if (!decimal.TryParse(
                    validation.Amount,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var validatedAmount))
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Invalid payment amount received.";

                await _context.SaveChangesAsync();

                return BadRequest();
            }

            if (validatedAmount != payment.Amount)
            {
                payment.Status = "Failed";

                payment.Remarks =
                    "Payment amount validation failed.";

                await _context.SaveChangesAsync();

                return BadRequest();
            }

            // =====================================================
            // Save SSLCommerz Details
            // =====================================================

            payment.ValidationId =
                validation.ValId;

            payment.BankTransactionId =
                validation.BankTranId;

            payment.CardType =
                validation.CardType;

            payment.CardBrand =
                validation.CardBrand;

            payment.CardIssuer =
                validation.CardIssuer;

            payment.Status =
                "Completed";

            payment.PaymentDate =
                DateTime.UtcNow;

            // =====================================================
            // UPDATE INVOICE
            // =====================================================

            var invoice =
                payment.Invoice;

            invoice.PaidAmount +=
                payment.Amount;

            if (invoice.PaidAmount >=
                invoice.GrandTotal)
            {
                invoice.PaidAmount =
                    invoice.GrandTotal;

                invoice.DueAmount =
                    0;

                invoice.Status =
                    "Paid";
            }
            else
            {
                invoice.DueAmount =
                    invoice.GrandTotal -
                    invoice.PaidAmount;

                invoice.Status =
                    "Partially Paid";
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // =========================================================
        // PAYMENT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var payment =
                await _context.Payments
                    .Include(x => x.Invoice)
                    .ThenInclude(x => x.SalesOrder)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            var isFinance =
                User.IsInRole(
                    "FinanceLogisticsManager") ||
                User.IsInRole("Admin");

            // Buyer can only see own payment
            if (!isFinance)
            {
                if (payment.Invoice.SalesOrder == null ||
                    payment.Invoice.SalesOrder.BuyerId !=
                    user.Id)
                {
                    return Forbid();
                }
            }

            return View(payment);
        }
    }
}