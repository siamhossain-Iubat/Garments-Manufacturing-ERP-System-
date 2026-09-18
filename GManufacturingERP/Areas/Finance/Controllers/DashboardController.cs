using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // FINANCE & LOGISTICS DASHBOARD
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // =====================================================
            // INVOICE STATISTICS
            // =====================================================

            var totalInvoices = await _context.Invoices
                .CountAsync();

            var issuedInvoices = await _context.Invoices
                .CountAsync(x => x.Status == "Issued");

            var paidInvoices = await _context.Invoices
                .CountAsync(x => x.Status == "Paid");

            var partiallyPaidInvoices = await _context.Invoices
                .CountAsync(x => x.Status == "Partially Paid");

            var pendingInvoices = await _context.Invoices
                .CountAsync(x =>
                    x.Status == "Issued" ||
                    x.Status == "Partially Paid");


            // =====================================================
            // FINANCIAL AMOUNTS
            // =====================================================

            var totalInvoicedAmount = await _context.Invoices
                .Where(x => x.Status != "Cancelled")
                .SumAsync(x => (decimal?)x.GrandTotal) ?? 0m;

            var totalPaidAmount = await _context.Invoices
                .Where(x => x.Status != "Cancelled")
                .SumAsync(x => (decimal?)x.PaidAmount) ?? 0m;

            var totalDueAmount = await _context.Invoices
                .Where(x => x.Status != "Cancelled")
                .SumAsync(x => (decimal?)x.DueAmount) ?? 0m;


            // =====================================================
            // PAYMENT STATISTICS
            // =====================================================

            var pendingPayments = await _context.Payments
                .CountAsync(x => x.Status == "Pending");

            var completedPayments = await _context.Payments
                .CountAsync(x =>
                    x.Status == "Completed" ||
                    x.Status == "Success" ||
                    x.Status == "Successful");


            // =====================================================
            // LOGISTICS STATISTICS
            // =====================================================

            var totalShipments = await _context.Shipments
                .CountAsync();

            var activeShipments = await _context.Shipments
                .CountAsync(x => x.Status != "Delivered");


            var pendingDispatches = await _context.Dispatches
                .CountAsync(x =>
                    x.Status == "Draft" ||
                    x.Status == "Pending");


            var pendingDeliveries = await _context.Deliveries
                .CountAsync(x =>
                    x.Status == "Pending");


            var completedDeliveries = await _context.Deliveries
                .CountAsync(x =>
                    x.Status == "Delivered");


            // =====================================================
            // PACKING STATISTICS
            // =====================================================

            var pendingPacking = await _context.Packings
                .CountAsync(x =>
                    x.Status == "Draft");


            // =====================================================
            // SEND DATA TO VIEW
            // =====================================================

            ViewBag.TotalInvoices = totalInvoices;
            ViewBag.IssuedInvoices = issuedInvoices;
            ViewBag.PaidInvoices = paidInvoices;
            ViewBag.PartiallyPaidInvoices = partiallyPaidInvoices;
            ViewBag.PendingInvoices = pendingInvoices;

            ViewBag.TotalInvoicedAmount = totalInvoicedAmount;
            ViewBag.TotalPaidAmount = totalPaidAmount;
            ViewBag.TotalDueAmount = totalDueAmount;

            ViewBag.PendingPayments = pendingPayments;
            ViewBag.CompletedPayments = completedPayments;

            ViewBag.TotalShipments = totalShipments;
            ViewBag.ActiveShipments = activeShipments;

            ViewBag.PendingDispatches = pendingDispatches;
            ViewBag.PendingDeliveries = pendingDeliveries;
            ViewBag.CompletedDeliveries = completedDeliveries;

            ViewBag.PendingPacking = pendingPacking;


            // =====================================================
            // RECENT ACTIVITIES
            // =====================================================

            var recentInvoices = await _context.Invoices
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    Type = "Invoice",
                    Reference = x.InvoiceNumber,
                    Description = "Invoice created",
                    Status = x.Status,
                    Date = x.CreatedAt
                })
                .ToListAsync();


            var recentPayments = await _context.Payments
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    Type = "Payment",
                    Reference = x.PaymentNumber,
                    Description = "Payment received",
                    Status = x.Status,
                    Date = x.CreatedAt
                })
                .ToListAsync();


            var recentShipments = await _context.Shipments
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    Type = "Shipment",
                    Reference = x.ShipmentNumber,
                    Description = "Shipment created",
                    Status = x.Status,
                    Date = x.CreatedAt
                })
                .ToListAsync();


            var recentActivities = recentInvoices
                .Concat(recentPayments)
                .Concat(recentShipments)
                .OrderByDescending(x => x.Date)
                .Take(8)
                .ToList();


            ViewBag.RecentActivities = recentActivities;


            return View();
        }
    }
}