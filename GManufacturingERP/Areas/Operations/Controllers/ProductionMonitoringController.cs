using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Operations.Controllers
{
    [Area("Operations")]
    [Authorize(Roles = "OperationsManager")]
    public class ProductionMonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionMonitoringController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productions = await _context.ProductionMonitorings
                .Include(x => x.ProgressEntries)
                .Include(x => x.ProductionPlan)
                    .ThenInclude(x => x!.SalesOrder)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(productions);
        }


        // =====================================================
        // CREATE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateProductionMonitoringViewModel
            {
                StartDate = DateTime.Today
            };

            await LoadApprovedMaterialIssues(model);

            return View(model);
        }


        // =====================================================
        // CREATE FROM APPROVED MATERIAL ISSUE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CreateFromMaterialIssue(
            int materialIssueId)
        {
            var materialIssue = await _context.MaterialIssues
                .Include(x => x.Items)
                .Include(x => x.MaterialRequirementPlan)
                    .ThenInclude(x => x!.ProductionPlan)
                        .ThenInclude(x => x!.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == materialIssueId);

            if (materialIssue == null)
            {
                TempData["Error"] = "Material issue not found.";

                return RedirectToAction(nameof(Create));
            }

            // Only approved Material Issue can start production
            if (materialIssue.Status != "Approved")
            {
                TempData["Error"] =
                    "Only approved material issues can be used for production.";

                return RedirectToAction(nameof(Create));
            }

            // -------------------------------------------------
            // Get Production Plan
            // -------------------------------------------------

            var productionPlan =
                materialIssue.MaterialRequirementPlan?.ProductionPlan;

            if (productionPlan == null)
            {
                TempData["Error"] =
                    "Production plan linked with this material issue was not found.";

                return RedirectToAction(nameof(Create));
            }

            // -------------------------------------------------
            // Get Sales Order
            // -------------------------------------------------

            var salesOrder = productionPlan.SalesOrder;

            if (salesOrder == null)
            {
                TempData["Error"] =
                    "Sales order linked with this production plan was not found.";

                return RedirectToAction(nameof(Create));
            }

            // -------------------------------------------------
            // Prevent duplicate monitoring
            // -------------------------------------------------

            var alreadyExists = await _context.ProductionMonitorings
                .AnyAsync(x =>
                    x.ProductionReference ==
                    materialIssue.ProductionReference);

            if (alreadyExists)
            {
                TempData["Error"] =
                    "Production monitoring already exists for this production reference.";

                return RedirectToAction(nameof(Index));
            }

            // -------------------------------------------------
            // Validate issued material quantity
            // -------------------------------------------------

            var issuedQuantity = materialIssue.Items
                .Sum(x => x.IssuedQuantity);

            if (issuedQuantity <= 0)
            {
                TempData["Error"] =
                    "This material issue has no issued quantity.";

                return RedirectToAction(nameof(Create));
            }

            // -------------------------------------------------
            // Product information comes from Sales Order
            // -------------------------------------------------

            var productName = salesOrder.ProductDescription;

            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = productionPlan.ProductDescription;
            }

            var plannedQuantity = productionPlan.PlannedQuantity;

            var unit = salesOrder.Unit;

            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "Pieces";
            }

            // -------------------------------------------------
            // Prepare ViewModel
            // -------------------------------------------------

            var model = new CreateProductionMonitoringViewModel
            {
                MaterialIssueId = materialIssue.Id,

                ProductionReference =
                    materialIssue.ProductionReference,

                ProductName =
                    productName,

                PlannedQuantity =
                    plannedQuantity,

                Unit =
                    unit,

                StartDate =
                    DateTime.Today,

                Remarks =
                    null
            };

            await LoadApprovedMaterialIssues(model);

            return View("Create", model);
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateProductionMonitoringViewModel model)
        {
            // -------------------------------------------------
            // Find selected Material Issue
            // -------------------------------------------------

            var materialIssue = await _context.MaterialIssues
                .Include(x => x.Items)
                .Include(x => x.MaterialRequirementPlan)
                    .ThenInclude(x => x!.ProductionPlan)
                        .ThenInclude(x => x!.SalesOrder)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.MaterialIssueId);

            if (materialIssue == null)
            {
                ModelState.AddModelError(
                    "MaterialIssueId",
                    "Please select a valid material issue.");

                await LoadApprovedMaterialIssues(model);

                return View(model);
            }

            // -------------------------------------------------
            // Only Approved Material Issue can start production
            // -------------------------------------------------

            if (materialIssue.Status != "Approved")
            {
                ModelState.AddModelError(
                    "MaterialIssueId",
                    "Only approved material issues can be used for production.");

                await LoadApprovedMaterialIssues(model);

                return View(model);
            }

            // -------------------------------------------------
            // Get Production Plan
            // -------------------------------------------------

            var productionPlan =
                materialIssue.MaterialRequirementPlan?.ProductionPlan;

            if (productionPlan == null)
            {
                ModelState.AddModelError(
                    "",
                    "Production plan linked with this material issue was not found.");

                await LoadApprovedMaterialIssues(model);

                return View(model);
            }

            // -------------------------------------------------
            // Get Sales Order
            // -------------------------------------------------

            var salesOrder = productionPlan.SalesOrder;

            if (salesOrder == null)
            {
                ModelState.AddModelError(
                    "",
                    "Sales order linked with this production plan was not found.");

                await LoadApprovedMaterialIssues(model);

                return View(model);
            }

            // -------------------------------------------------
            // Prevent duplicate monitoring
            // -------------------------------------------------

            var alreadyExists = await _context.ProductionMonitorings
                .AnyAsync(x =>
                    x.ProductionReference ==
                    materialIssue.ProductionReference);

            if (alreadyExists)
            {
                TempData["Error"] =
                    "Production monitoring already exists for this production reference.";

                return RedirectToAction(nameof(Index));
            }

            // -------------------------------------------------
            // Validate issued quantity
            // -------------------------------------------------

            var issuedQuantity = materialIssue.Items
                .Sum(x => x.IssuedQuantity);

            if (issuedQuantity <= 0)
            {
                TempData["Error"] =
                    "This material issue has no issued quantity.";

                return RedirectToAction(nameof(Create));
            }

            // -------------------------------------------------
            // Trusted Production Information
            // -------------------------------------------------

            var productName =
                salesOrder.ProductDescription;

            if (string.IsNullOrWhiteSpace(productName))
            {
                productName =
                    productionPlan.ProductDescription;
            }

            var plannedQuantity =
                productionPlan.PlannedQuantity;

            var unit =
                salesOrder.Unit;

            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "Pieces";
            }

            // -------------------------------------------------
            // Validate Start Date
            // -------------------------------------------------

            if (model.StartDate == default)
            {
                ModelState.AddModelError(
                    "StartDate",
                    "Start date is required.");
            }

            if (!ModelState.IsValid)
            {
                // Restore trusted values
                model.ProductionReference =
                    materialIssue.ProductionReference;

                model.ProductName =
                    productName;

                model.PlannedQuantity =
                    plannedQuantity;

                model.Unit =
                    unit;

                await LoadApprovedMaterialIssues(model);

                return View(model);
            }

            // -------------------------------------------------
            // Create Production Monitoring
            // -------------------------------------------------

            var monitoring = new ProductionMonitoring
            {
                MonitoringNumber =
                    GenerateMonitoringNumber(),

                ProductionReference =
                    materialIssue.ProductionReference,

                ProductionPlanId =
                    productionPlan.Id,

                ProductName =
                    productName,

                PlannedQuantity =
                    plannedQuantity,

                ProducedQuantity =
                    0,

                RejectedQuantity =
                    0,

                RemainingQuantity =
                    plannedQuantity,

                Unit =
                    unit,

                StartDate =
                    model.StartDate,

                Status =
                    "Not Started",

                Remarks =
                    model.Remarks,

                CreatedBy =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? User.Identity?.Name
                    ?? "System",

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.ProductionMonitorings.Add(monitoring);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Production monitoring {monitoring.MonitoringNumber} created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // LOAD APPROVED MATERIAL ISSUES
        // =====================================================

        private async Task LoadApprovedMaterialIssues(
            CreateProductionMonitoringViewModel model)
        {
            var approvedIssues =
                await _context.MaterialIssues
                    .Where(x => x.Status == "Approved")
                    .OrderByDescending(x => x.IssueDate)
                    .ToListAsync();

            model.ApprovedMaterialIssues =
                approvedIssues
                    .Select(x => new SelectListItem
                    {
                        Value =
                            x.Id.ToString(),

                        Text =
                            $"{x.IssueNumber} - {x.ProductionReference}",

                        Selected =
                            x.Id == model.MaterialIssueId
                    })
                    .ToList();
        }


        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var production =
                await _context.ProductionMonitorings
                    .Include(x => x.ProgressEntries)
                    .Include(x => x.ProductionPlan)
                        .ThenInclude(x => x!.SalesOrder)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (production == null)
            {
                return NotFound();
            }

            // Load Rework record
            ViewBag.Rework =
                await _context.FinishedGoodsReworks
                    .Include(x => x.FinishedGoodsStock)
                    .FirstOrDefaultAsync(x =>
                        x.ProductionMonitoringId == production.Id);

            return View(production);
        }


        // =====================================================
        // ADD PROGRESS - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AddProgress(int id)
        {
            var production =
                await _context.ProductionMonitorings
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (production == null)
            {
                return NotFound();
            }

            // Completed production cannot receive more progress
            if (production.Status == "Completed")
            {
                TempData["Error"] =
                    "Completed production cannot receive more progress.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var model =
                new CreateProductionProgressViewModel
                {
                    ProductionMonitoringId =
                        production.Id,

                    ProductionReference =
                        production.ProductionReference,

                    ProductName =
                        production.ProductName,

                    PlannedQuantity =
                        production.PlannedQuantity,

                    ProducedQuantity =
                        production.ProducedQuantity,

                    RejectedQuantity =
                        production.RejectedQuantity,

                    RemainingQuantity =
                        production.RemainingQuantity
                };

            return View(model);
        }


        // =====================================================
        // ADD PROGRESS - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProgress(
            CreateProductionProgressViewModel model)
        {
            var production =
                await _context.ProductionMonitorings
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.ProductionMonitoringId);

            if (production == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // Completed production cannot be updated
            // -------------------------------------------------

            if (production.Status == "Completed")
            {
                TempData["Error"] =
                    "Completed production cannot receive more progress.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = production.Id });
            }

            // -------------------------------------------------
            // Basic validation
            // -------------------------------------------------

            if (model.TodayProducedQuantity <= 0)
            {
                ModelState.AddModelError(
                    "TodayProducedQuantity",
                    "Today's produced quantity must be greater than zero.");
            }

            if (model.TodayRejectedQuantity < 0)
            {
                ModelState.AddModelError(
                    "TodayRejectedQuantity",
                    "Today's rejected quantity cannot be negative.");
            }

            // Rejected quantity cannot be greater than
            // today's total production.
            if (model.TodayRejectedQuantity >
                model.TodayProducedQuantity)
            {
                ModelState.AddModelError(
                    "TodayRejectedQuantity",
                    "Rejected quantity cannot exceed today's produced quantity.");
            }

            // -------------------------------------------------
            // Calculate today's GOOD quantity
            //
            // Example:
            // Produced = 1000
            // Rejected = 200
            // Good     = 800
            // -------------------------------------------------

            var todayGoodQuantity =
                model.TodayProducedQuantity -
                model.TodayRejectedQuantity;

            // -------------------------------------------------
            // Today's good quantity cannot exceed remaining
            // required good quantity.
            // -------------------------------------------------

            if (todayGoodQuantity >
                production.RemainingQuantity)
            {
                ModelState.AddModelError(
                    "TodayProducedQuantity",
                    "Today's good production cannot exceed the remaining required quantity.");
            }

            // -------------------------------------------------
            // Model validation
            // -------------------------------------------------

            if (!ModelState.IsValid)
            {
                model.ProductionReference =
                    production.ProductionReference;

                model.ProductName =
                    production.ProductName;

                model.PlannedQuantity =
                    production.PlannedQuantity;

                model.ProducedQuantity =
                    production.ProducedQuantity;

                model.RejectedQuantity =
                    production.RejectedQuantity;

                model.RemainingQuantity =
                    production.RemainingQuantity;

                return View(model);
            }

            // -------------------------------------------------
            // Update total production
            // -------------------------------------------------

            production.ProducedQuantity +=
                model.TodayProducedQuantity;

            production.RejectedQuantity +=
                model.TodayRejectedQuantity;

            // -------------------------------------------------
            // GOOD QUANTITY
            //
            // Total Good =
            // Total Produced - Total Rejected
            // -------------------------------------------------

            var totalGoodQuantity =
                production.ProducedQuantity -
                production.RejectedQuantity;

            // -------------------------------------------------
            // Remaining GOOD quantity
            //
            // Planned - Good Produced
            // -------------------------------------------------

            production.RemainingQuantity =
                production.PlannedQuantity -
                totalGoodQuantity;

            // Prevent negative remaining quantity
            if (production.RemainingQuantity < 0)
            {
                production.RemainingQuantity = 0;
            }

            // -------------------------------------------------
            // Completion Logic
            //
            // Production is completed ONLY when the required
            // GOOD quantity has been achieved.
            // -------------------------------------------------

            if (totalGoodQuantity >=
                production.PlannedQuantity)
            {
                production.RemainingQuantity = 0;

                production.Status =
                    "Completed";

                production.CompletionDate =
                    DateTime.UtcNow;
            }
            else
            {
                production.Status =
                    "In Progress";
            }

            // -------------------------------------------------
            // Create Production Progress History
            // -------------------------------------------------

            var entry =
                new ProductionProgressEntry
                {
                    ProductionMonitoringId =
                        production.Id,

                    EntryDate =
                        DateTime.UtcNow,

                    ProducedQuantity =
                        model.TodayProducedQuantity,

                    RejectedQuantity =
                        model.TodayRejectedQuantity,

                    TotalProducedAfterEntry =
                        production.ProducedQuantity,

                    RemainingQuantityAfterEntry =
                        production.RemainingQuantity,

                    Remarks =
                        model.Remarks,

                    EnteredBy =
                        User.FindFirstValue(ClaimTypes.Name)
                        ?? User.Identity?.Name
                        ?? "System"
                };

            _context.ProductionProgressEntries.Add(entry);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Production progress added successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = production.Id });
        }


        // =====================================================
        // MONITORING NUMBER
        // =====================================================

        private static string GenerateMonitoringNumber()
        {
            return $"PM-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}