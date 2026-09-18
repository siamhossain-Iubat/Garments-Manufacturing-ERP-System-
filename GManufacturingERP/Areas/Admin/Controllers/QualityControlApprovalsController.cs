using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class QualityControlApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QualityControlApprovalsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pending =
                await _context.QualityInspections
                    .Include(x =>
                        x.ProductionMonitoring)
                    .Where(x =>
                        x.Status == "Submitted")
                    .OrderByDescending(x =>
                        x.InspectionDate)
                    .ToListAsync();


            var history =
                await _context.QualityInspections
                    .Include(x =>
                        x.ProductionMonitoring)
                    .Where(x =>
                        x.Status == "Approved"
                        ||
                        x.Status == "Rejected")
                    .OrderByDescending(x =>
                        x.InspectionDate)
                    .ToListAsync();


            ViewBag.History = history;

            return View(pending);
        }


        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var inspection =
                await _context.QualityInspections
                    .Include(x =>
                        x.ProductionMonitoring)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }

            return View(inspection);
        }


        // =====================================================
        // APPROVE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id)
        {
            var inspection =
                await _context.QualityInspections
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }


            if (inspection.Status != "Submitted")
            {
                TempData["Error"] =
                    "Only submitted inspections can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            // -------------------------------------------------
            // Find Production Monitoring
            // -------------------------------------------------

            var production =
                await _context.ProductionMonitorings
                    .Include(x =>
                        x.ProductionPlan)
                        .ThenInclude(x =>
                            x!.SalesOrder)
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        inspection.ProductionMonitoringId);

            if (production == null)
            {
                TempData["Error"] =
                    "Production monitoring was not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            if (production.Status != "Completed")
            {
                TempData["Error"] =
                    "Only completed production can be approved by QC.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            try
            {
                // -------------------------------------------------
                // Approve QC
                // -------------------------------------------------

                inspection.Status =
                    "Approved";

                inspection.ApprovedBy =
                    User.FindFirstValue(
                        ClaimTypes.Name)
                    ?? User.Identity?.Name
                    ?? "System";

                inspection.ApprovedAt =
                    DateTime.UtcNow;


                // -------------------------------------------------
                // Sync Finished Goods
                // -------------------------------------------------

                await SyncFinishedGoodsFromQC(
                    inspection.Id);


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    $"QC {inspection.InspectionNumber} approved and Finished Goods updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "QC approval failed: " + ex.Message;
            }


            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =====================================================
        // REJECT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? rejectionReason)
        {
            var inspection =
                await _context.QualityInspections
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }


            if (inspection.Status != "Submitted")
            {
                TempData["Error"] =
                    "Only submitted inspections can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            inspection.Status =
                "Rejected";


            if (!string.IsNullOrWhiteSpace(
                rejectionReason))
            {
                inspection.Remarks =
                    $"Rejection Reason: {rejectionReason}\n"
                    + inspection.Remarks;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"QC {inspection.InspectionNumber} rejected.";


            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =====================================================
        // FINISHED GOODS SYNC
        // =====================================================

        private async Task SyncFinishedGoodsFromQC(
            int qualityInspectionId)
        {
            var inspection =
                await _context.QualityInspections
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        qualityInspectionId);

            if (inspection == null)
            {
                throw new Exception(
                    "Quality inspection was not found.");
            }


            // -------------------------------------------------
            // Prevent duplicate sync
            // -------------------------------------------------

            var alreadyProcessed =
                await _context.FinishedGoodsTransactions
                    .AnyAsync(x =>
                        x.ReferenceNumber ==
                        inspection.InspectionNumber
                        &&
                        (
                            x.TransactionType ==
                            "FG_RECEIPT"

                            ||

                            x.TransactionType ==
                            "REWORK_RECEIPT"
                        ));

            if (alreadyProcessed)
            {
                return;
            }


            // -------------------------------------------------
            // Find Production Monitoring
            // -------------------------------------------------

            var production =
                await _context.ProductionMonitorings
                    .Include(x =>
                        x.ProductionPlan)
                        .ThenInclude(x =>
                            x!.SalesOrder)
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        inspection.ProductionMonitoringId);

            if (production == null)
            {
                throw new Exception(
                    "Production monitoring was not found.");
            }


            var productName =
                inspection.ProductName.Trim();

            var unit = "PCS";


            var productionPlanId =
                production.ProductionPlanId;

            var salesOrderId =
                production.ProductionPlan?.SalesOrderId;


            var orderedQuantity =
                production
                    .ProductionPlan?
                    .SalesOrder?
                    .Quantity ?? 0;


            // -------------------------------------------------
            // Find existing Finished Goods Stock
            // -------------------------------------------------

            var stock =
                await _context.FinishedGoodsStocks
                    .FirstOrDefaultAsync(x =>
                        x.ProductName ==
                            productName
                        &&
                        x.Unit ==
                            unit
                        &&
                        x.ProductionPlanId ==
                            productionPlanId);


            // -------------------------------------------------
            // Create Finished Goods Stock
            // -------------------------------------------------

            if (stock == null)
            {
                stock =
                    new FinishedGoodsStock
                    {
                        ProductName =
                            productName,

                        Unit =
                            unit,

                        ProductionPlanId =
                            productionPlanId,

                        SalesOrderId =
                            salesOrderId,

                        ProductionReference =
                            inspection.ProductionReference,

                        OrderedQuantity =
                            orderedQuantity,

                        AvailableQuantity =
                            0,

                        ReservedQuantity =
                            0,

                        ReworkQuantity =
                            0,

                        Location =
                            "Finished Good warehouse",

                        UpdatedAt =
                            DateTime.UtcNow
                    };

                _context.FinishedGoodsStocks.Add(
                    stock);
            }


            // -------------------------------------------------
            // Check Rework
            // -------------------------------------------------

            var rework =
                await _context.FinishedGoodsReworks
                    .Include(x =>
                        x.FinishedGoodsStock)
                    .FirstOrDefaultAsync(x =>
                        x.ProductionMonitoringId ==
                        inspection.ProductionMonitoringId);


            // =================================================
            // REWORK PRODUCTION
            // =================================================

            if (rework != null)
            {
                var originalStock =
                    rework.FinishedGoodsStock;

                if (originalStock == null)
                {
                    throw new Exception(
                        "Original Finished Goods stock was not found.");
                }


                // Passed rework quantity
                originalStock.AvailableQuantity +=
                    inspection.PassedQuantity;


                // Failed rework quantity
                originalStock.ReworkQuantity +=
                    inspection.FailedQuantity;


                originalStock.UpdatedAt =
                    DateTime.UtcNow;


                rework.PassedQuantity =
                    inspection.PassedQuantity;

                rework.RejectedQuantity =
                    inspection.FailedQuantity;


                rework.Status =
                    inspection.FailedQuantity > 0
                        ? "Partially Completed"
                        : "Completed";


                rework.CompletedAt =
                    DateTime.UtcNow;


                // Rework receipt transaction
                if (inspection.PassedQuantity > 0)
                {
                    _context.FinishedGoodsTransactions.Add(
                        new FinishedGoodsTransaction
                        {
                            FinishedGoodsStockId =
                                originalStock.Id,

                            TransactionType =
                                "REWORK_RECEIPT",

                            ReferenceNumber =
                                inspection.InspectionNumber,

                            QuantityIn =
                                inspection.PassedQuantity,

                            QuantityOut =
                                0,

                            BalanceAfter =
                                originalStock.AvailableQuantity,

                            TransactionDate =
                                DateTime.UtcNow,

                            PerformedBy =
                                User.FindFirstValue(
                                    ClaimTypes.Name)
                                ?? User.Identity?.Name
                                ?? "System",

                            Remarks =
                                $"Rework QC received from {inspection.InspectionNumber}"
                        });
                }
            }


            // =================================================
            // NORMAL PRODUCTION
            // =================================================

            else
            {
                // Add passed quantity to Finished Goods
                stock.AvailableQuantity +=
                    inspection.PassedQuantity;


                // Add failed quantity to Rework
                stock.ReworkQuantity +=
                    inspection.FailedQuantity;


                stock.UpdatedAt =
                    DateTime.UtcNow;


                // -------------------------------------------------
                // Finished Goods Receipt
                // -------------------------------------------------

                if (inspection.PassedQuantity > 0)
                {
                    _context.FinishedGoodsTransactions.Add(
                        new FinishedGoodsTransaction
                        {
                            FinishedGoodsStock =
                                stock,

                            TransactionType =
                                "FG_RECEIPT",

                            ReferenceNumber =
                                inspection.InspectionNumber,

                            QuantityIn =
                                inspection.PassedQuantity,

                            QuantityOut =
                                0,

                            BalanceAfter =
                                stock.AvailableQuantity,

                            TransactionDate =
                                DateTime.UtcNow,

                            PerformedBy =
                                User.FindFirstValue(
                                    ClaimTypes.Name)
                                ?? User.Identity?.Name
                                ?? "System",

                            Remarks =
                                $"Finished Goods received from QC {inspection.InspectionNumber}"
                        });
                }


                // -------------------------------------------------
                // Rework Transaction
                // -------------------------------------------------

                if (inspection.FailedQuantity > 0)
                {
                    _context.FinishedGoodsTransactions.Add(
                        new FinishedGoodsTransaction
                        {
                            FinishedGoodsStock =
                                stock,

                            TransactionType =
                                "REWORK_RECEIPT",

                            ReferenceNumber =
                                inspection.InspectionNumber,

                            QuantityIn =
                                0,

                            QuantityOut =
                                0,

                            BalanceAfter =
                                stock.AvailableQuantity,

                            TransactionDate =
                                DateTime.UtcNow,

                            PerformedBy =
                                User.FindFirstValue(
                                    ClaimTypes.Name)
                                ?? User.Identity?.Name
                                ?? "System",

                            Remarks =
                                $"Rework quantity added from QC. Failed: {inspection.FailedQuantity} {unit}"
                        });
                }
            }
        }
    }
}