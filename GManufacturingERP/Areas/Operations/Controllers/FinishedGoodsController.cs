using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Operations.Controllers
{
    [Area("Operations")]
    [Authorize(Roles = "OperationsManager")]
    public class FinishedGoodsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinishedGoodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // =====================================================
        // INDEX
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var stocks = await _context.FinishedGoodsStocks
                .Include(x => x.ProductionPlan)
                    .ThenInclude(x => x!.SalesOrder)
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            return View(stocks);
        }

        // =====================================================
        // DETAILS
        // =====================================================

        public async Task<IActionResult> Details(int id)
        {
            var stock = await _context.FinishedGoodsStocks
                .Include(x => x.ProductionPlan)
                    .ThenInclude(x => x!.SalesOrder)
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null)
            {
                return NotFound();
            }

            stock.Transactions = stock.Transactions
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return View(stock);
        }

        // =====================================================
        // REWORK - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Rework(int id)
        {
            var stock = await _context.FinishedGoodsStocks
                .Include(x => x.ProductionPlan)
                    .ThenInclude(x => x!.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null)
            {
                return NotFound();
            }

            if (stock.ReworkQuantity <= 0)
            {
                TempData["Error"] =
                    "There is no quantity available for rework.";

                return RedirectToAction(nameof(Index));
            }

            var model = new ReworkFinishedGoodsViewModel
            {
                FinishedGoodsStockId = stock.Id,
                ProductName = stock.ProductName,
                Unit = stock.Unit,
                OrderedQuantity = stock.OrderedQuantity,
                AvailableQuantity = stock.AvailableQuantity,
                ReworkQuantity = stock.ReworkQuantity,
                ProductionReference = stock.ProductionReference,
                Quantity = stock.ReworkQuantity
            };

            return View(model);
        }

        // =====================================================
        // REWORK - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rework(
            ReworkFinishedGoodsViewModel model)
        {
            var stock = await _context.FinishedGoodsStocks
                .FirstOrDefaultAsync(x =>
                    x.Id == model.FinishedGoodsStockId);

            if (stock == null)
            {
                return NotFound();
            }

            if (model.Quantity <= 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Rework quantity must be greater than zero.");
            }

            if (model.Quantity > stock.ReworkQuantity)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Rework quantity cannot exceed available rework quantity.");
            }

            if (!ModelState.IsValid)
            {
                model.ProductName = stock.ProductName;
                model.Unit = stock.Unit;
                model.OrderedQuantity = stock.OrderedQuantity;
                model.AvailableQuantity = stock.AvailableQuantity;
                model.ReworkQuantity = stock.ReworkQuantity;
                model.ProductionReference =
                    stock.ProductionReference;

                return View(model);
            }

            var now = DateTime.UtcNow;

            var reworkNumber =
                $"RW-{now:yyyyMMddHHmmssfff}";

            var reworkProductionReference =
                $"REWORK-{reworkNumber}";

            // =================================================
            // CREATE PRODUCTION MONITORING FOR REWORK
            // =================================================

            var monitoring = new ProductionMonitoring
            {
                MonitoringNumber =
                    $"PM-{now:yyyyMMddHHmmssfff}",

                ProductionReference =
                    reworkProductionReference,

                ProductName = stock.ProductName,

                PlannedQuantity = model.Quantity,

                ProducedQuantity = 0,

                RejectedQuantity = 0,

                RemainingQuantity = model.Quantity,

                Unit = stock.Unit,

                StartDate = DateTime.Today,

                Status = "Not Started",

                Remarks =
                    $"Rework created from FG stock. " +
                    $"Original reference: {stock.ProductionReference}. " +
                    $"{model.Remarks}",

                CreatedBy = CurrentUser(),

                CreatedAt = now,

                ProductionPlanId =
                    stock.ProductionPlanId
            };

            _context.ProductionMonitorings.Add(monitoring);

            // =================================================
            // CREATE REWORK RECORD
            // =================================================

            var rework = new FinishedGoodsRework
            {
                FinishedGoodsStockId = stock.Id,

                ProductionMonitoring = monitoring,

                ReworkNumber = reworkNumber,

                ProductName = stock.ProductName,

                Quantity = model.Quantity,

                PassedQuantity = 0,

                RejectedQuantity = 0,

                Unit = stock.Unit,

                Status = "In Production",

                Remarks = model.Remarks,

                CreatedBy = CurrentUser(),

                CreatedAt = now
            };

            _context.FinishedGoodsReworks.Add(rework);

            // =================================================
            // REMOVE FROM REWORK STOCK
            // =================================================

            stock.ReworkQuantity -= model.Quantity;
            stock.UpdatedAt = now;

            // =================================================
            // CREATE REWORK ISSUE TRANSACTION
            // =================================================

            var transaction = new FinishedGoodsTransaction
            {
                FinishedGoodsStockId = stock.Id,

                TransactionType = "REWORK_ISSUE",

                ReferenceNumber = reworkNumber,

                QuantityIn = 0,

                QuantityOut = model.Quantity,

                BalanceAfter = stock.AvailableQuantity,

                TransactionDate = now,

                PerformedBy = CurrentUser(),

                Remarks =
                    $"Rework started for {model.Quantity} {stock.Unit}. " +
                    $"Production reference: {reworkProductionReference}"
            };

            _context.FinishedGoodsTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Rework started successfully. " +
                $"Rework No: {reworkNumber}. " +
                $"Production Monitoring has been created.";

            return RedirectToAction(
                "Details",
                "ProductionMonitoring",
                new
                {
                    area = "Operations",
                    id = monitoring.Id
                });
        }
    }
}