
using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager,Admin")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CURRENT USER
        // =========================================================

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // =========================================================
        // GET SUCCESSFUL ADVANCE PAYMENT
        // =========================================================

        private async Task<decimal> GetAdvancePaidAmountAsync(
            int salesOrderId)
        {
            var advancePaid = await _context.AdvancePayments
                .Where(p =>
                    p.Status == "Paid" &&
                    p.Quotation != null &&
                    p.Quotation.SalesOrderId == salesOrderId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            return advancePaid;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(x => x.Delivery)
                    .ThenInclude(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                .Include(x => x.SalesOrder)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(invoices);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Delivery)
                    .ThenInclude(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                            .ThenInclude(x => x.Packing)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (invoice == null)
                return NotFound();

            return View(invoice);
        }

        // =========================================================
        // CREATE INVOICE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int deliveryId)
        {
            var delivery = await _context.Deliveries
                .Include(x => x.Dispatch)
                    .ThenInclude(x => x.Shipment)
                        .ThenInclude(x => x.Packing)
                            .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == deliveryId);

            if (delivery == null)
                return NotFound();

            if (delivery.Status != "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Invoice can only be created after the delivery is completed.";

                return RedirectToAction(
                    "Details",
                    "Delivery",
                    new
                    {
                        area = "Finance",
                        id = deliveryId
                    });
            }

            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.DeliveryId == deliveryId);

            if (existingInvoice != null)
            {
                TempData["ErrorMessage"] =
                    "An invoice already exists for this delivery.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = existingInvoice.Id });
            }

            var salesOrder = delivery.Dispatch?
                .Shipment?
                .Packing?
                .SalesOrder;

            if (salesOrder == null)
            {
                TempData["ErrorMessage"] =
                    "Sales Order could not be found for this delivery.";

                return RedirectToAction(
                    "Details",
                    "Delivery",
                    new
                    {
                        area = "Finance",
                        id = deliveryId
                    });
            }

            var buyerName =
                !string.IsNullOrWhiteSpace(salesOrder.CompanyName)
                    ? salesOrder.CompanyName
                    : salesOrder.ContactPerson ?? "N/A";

            var model = new CreateInvoiceViewModel
            {
                // DELIVERY
                DeliveryId = delivery.Id,
                DeliveryNumber = delivery.DeliveryNumber,

                DispatchNumber =
                    delivery.Dispatch?.DispatchNumber ?? "N/A",

                ShipmentNumber =
                    delivery.Dispatch?.Shipment?.ShipmentNumber ?? "N/A",

                // SALES ORDER
                SalesOrderId = salesOrder.Id,
                SalesOrderNumber = salesOrder.OrderNumber,

                // BUYER
                BuyerName = buyerName,

                // PRODUCT
                ProductName = delivery.ProductName,
                Unit = delivery.Unit,
                Quantity = delivery.Quantity,

                // COMMERCIAL VALUES
                UnitPrice = salesOrder.UnitPrice,

                Discount = CalculateAllocatedAmount(
                    salesOrder.Discount,
                    salesOrder.Quantity,
                    delivery.Quantity),

                Tax = CalculateAllocatedAmount(
                    salesOrder.Tax,
                    salesOrder.Quantity,
                    delivery.Quantity),

                // DATES
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),

                Notes = null
            };

            // SUBTOTAL
            model.Subtotal =
                model.Quantity * model.UnitPrice;

            // GRAND TOTAL
            model.GrandTotal =
                model.Subtotal - model.Discount + model.Tax;

            // =====================================================
            // ADVANCE PAYMENT
            // =====================================================

            model.AdvancePaidAmount =
                await GetAdvancePaidAmountAsync(salesOrder.Id);

            model.AdvancePaidAmount = Math.Min(
                model.AdvancePaidAmount,
                model.GrandTotal);

            model.RemainingDue =
                model.GrandTotal - model.AdvancePaidAmount;

            return View(model);
        }

        // =========================================================
        // CREATE INVOICE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateInvoiceViewModel model)
        {
            // LOAD DELIVERY AGAIN FROM DATABASE

            var delivery = await _context.Deliveries
                .Include(x => x.Dispatch)
                    .ThenInclude(x => x.Shipment)
                        .ThenInclude(x => x.Packing)
                            .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == model.DeliveryId);

            if (delivery == null)
                return NotFound();

            // DELIVERY VALIDATION

            if (delivery.Status != "Delivered")
            {
                ModelState.AddModelError(
                    "",
                    "Invoice can only be created for a Delivered record.");
            }

            // DUPLICATE INVOICE VALIDATION

            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(
                    x => x.DeliveryId == model.DeliveryId);

            if (existingInvoice != null)
            {
                ModelState.AddModelError(
                    "",
                    "An invoice already exists for this delivery.");
            }

            // SALES ORDER

            var salesOrder = delivery.Dispatch?
                .Shipment?
                .Packing?
                .SalesOrder;

            if (salesOrder == null)
            {
                ModelState.AddModelError(
                    "",
                    "Sales Order could not be found for this delivery.");
            }

            // =====================================================
            // COMMERCIAL VALUES FROM DATABASE
            // =====================================================

            decimal unitPrice = 0m;
            decimal discount = 0m;
            decimal tax = 0m;
            decimal subtotal = 0m;
            decimal grandTotal = 0m;

            if (salesOrder != null)
            {
                unitPrice = salesOrder.UnitPrice;

                discount = CalculateAllocatedAmount(
                    salesOrder.Discount,
                    salesOrder.Quantity,
                    delivery.Quantity);

                tax = CalculateAllocatedAmount(
                    salesOrder.Tax,
                    salesOrder.Quantity,
                    delivery.Quantity);

                subtotal =
                    delivery.Quantity * unitPrice;

                grandTotal =
                    subtotal - discount + tax;

                if (unitPrice < 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Sales Order contains an invalid unit price.");
                }

                if (discount < 0 || tax < 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Sales Order contains invalid discount or tax values.");
                }

                if (discount > subtotal)
                {
                    ModelState.AddModelError(
                        "",
                        "Allocated discount cannot exceed the invoice subtotal.");
                }
            }

            // =====================================================
            // ADVANCE PAYMENT - SERVER SIDE
            // =====================================================

            decimal advancePaid = 0m;

            if (salesOrder != null)
            {
                advancePaid =
                    await GetAdvancePaidAmountAsync(salesOrder.Id);

                advancePaid = Math.Min(
                    advancePaid,
                    grandTotal);
            }

            decimal remainingDue =
                grandTotal - advancePaid;

            // =====================================================
            // DUE DATE VALIDATION
            // =====================================================

            if (model.DueDate.Date < model.InvoiceDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be earlier than invoice date.");
            }

            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                PopulateModelFromDelivery(
                    model,
                    delivery,
                    unitPrice,
                    discount,
                    tax,
                    subtotal,
                    grandTotal);

                // Restore calculated advance values
                model.AdvancePaidAmount = advancePaid;
                model.RemainingDue = remainingDue;

                return View(model);
            }

            // =====================================================
            // CREATE INVOICE
            // =====================================================

            var now = DateTime.UtcNow;

            var buyerName =
                !string.IsNullOrWhiteSpace(salesOrder!.CompanyName)
                    ? salesOrder.CompanyName
                    : salesOrder.ContactPerson ?? "N/A";

            var invoice = new Invoice
            {
                // INVOICE INFORMATION
                InvoiceNumber =
                    $"INV-{now:yyyyMMddHHmmssfff}",

                DeliveryId = delivery.Id,

                // SALES ORDER
                SalesOrderId = salesOrder.Id,

                // BUYER
                BuyerName = buyerName,

                // PRODUCT
                ProductName = delivery.ProductName,
                Unit = delivery.Unit,
                Quantity = delivery.Quantity,

                // COMMERCIAL VALUES
                UnitPrice = unitPrice,
                Subtotal = subtotal,
                Discount = discount,
                Tax = tax,
                GrandTotal = grandTotal,

                // PAYMENT
                PaidAmount = advancePaid,
                DueAmount = remainingDue,

                // DATE
                InvoiceDate = model.InvoiceDate,
                DueDate = model.DueDate,

                // STATUS
                Status = "Draft",

                // AUDIT
                CreatedBy = CurrentUser(),
                CreatedAt = now,
                IssuedAt = null,

                // NOTES
                Notes = model.Notes
            };

            // SAVE INVOICE

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Invoice {invoice.InvoiceNumber} created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = invoice.Id });
        }

        // =========================================================
        // ALLOCATE DISCOUNT / TAX FOR PARTIAL DELIVERY
        // =========================================================

        private decimal CalculateAllocatedAmount(
            decimal orderAmount,
            int orderQuantity,
            decimal deliveredQuantity)
        {
            if (orderAmount <= 0 ||
                orderQuantity <= 0 ||
                deliveredQuantity <= 0)
            {
                return 0;
            }

            // FULL DELIVERY

            if (deliveredQuantity >= orderQuantity)
            {
                return orderAmount;
            }

            // PARTIAL DELIVERY

            return Math.Round(
                orderAmount * deliveredQuantity / orderQuantity,
                2,
                MidpointRounding.AwayFromZero);
        }

        // =========================================================
        // POPULATE MODEL FROM DELIVERY + SALES ORDER
        // =========================================================

        private void PopulateModelFromDelivery(
            CreateInvoiceViewModel model,
            Delivery delivery,
            decimal unitPrice,
            decimal discount,
            decimal tax,
            decimal subtotal,
            decimal grandTotal)
        {
            var salesOrder = delivery.Dispatch?
                .Shipment?
                .Packing?
                .SalesOrder;

            // DELIVERY

            model.DeliveryNumber = delivery.DeliveryNumber;

            model.DispatchNumber =
                delivery.Dispatch?.DispatchNumber ?? "N/A";

            model.ShipmentNumber =
                delivery.Dispatch?.Shipment?.ShipmentNumber ?? "N/A";

            // SALES ORDER

            model.SalesOrderId =
                delivery.Dispatch?
                    .Shipment?
                    .Packing?
                    .SalesOrderId;

            model.SalesOrderNumber =
                salesOrder?.OrderNumber ?? "N/A";

            // BUYER

            model.BuyerName =
                !string.IsNullOrWhiteSpace(salesOrder?.CompanyName)
                    ? salesOrder.CompanyName
                    : salesOrder?.ContactPerson ?? "N/A";

            // PRODUCT

            model.ProductName = delivery.ProductName;
            model.Unit = delivery.Unit;
            model.Quantity = delivery.Quantity;

            // COMMERCIAL VALUES

            model.UnitPrice = unitPrice;
            model.Discount = discount;
            model.Tax = tax;
            model.Subtotal = subtotal;
            model.GrandTotal = grandTotal;
        }

        // =========================================================
        // ISSUE INVOICE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(int id)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (invoice == null)
                return NotFound();

            if (invoice.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft invoices can be issued.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            invoice.Status = "Issued";
            invoice.IssuedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Invoice {invoice.InvoiceNumber} issued successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // CANCEL INVOICE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (invoice == null)
                return NotFound();

            if (invoice.Status == "Paid")
            {
                TempData["ErrorMessage"] =
                    "Paid invoices cannot be cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (invoice.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "This invoice is already cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            invoice.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Invoice {invoice.InvoiceNumber} cancelled successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}