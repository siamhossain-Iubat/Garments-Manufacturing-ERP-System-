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
    public class MRPController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MRPController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // ALL MRP PLANS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(plans);
        }

        // =====================================================
        // PRODUCTION PLANS AVAILABLE FOR MRP
        // Only Admin-approved Production Plans
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ProductionPlans()
        {
            var plans = await _context.ProductionPlans
                .Where(x => x.ProductionStatus == "Planned")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(plans);
        }

        // =====================================================
        // CREATE MRP - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(int productionPlanId)
        {
            var productionPlan = await _context.ProductionPlans
                .FirstOrDefaultAsync(x =>
                    x.Id == productionPlanId &&
                    x.ProductionStatus == "Planned");

            if (productionPlan == null)
            {
                TempData["ErrorMessage"] =
                    "Only approved/planned production plans can be used to create an MRP.";

                return RedirectToAction(nameof(ProductionPlans));
            }

            // Prevent duplicate MRP for the same Production Plan
            var existingMRP = await _context.MaterialRequirementPlans
                .AnyAsync(x =>
                    x.ProductionPlanId == productionPlanId);

            if (existingMRP)
            {
                TempData["ErrorMessage"] =
                    "An MRP already exists for this production plan.";

                return RedirectToAction(nameof(ProductionPlans));
            }

            var model = new CreateMRPViewModel
            {
                ProductionPlanId = productionPlan.Id,
                PlanNumber = productionPlan.PlanNumber,
                ProductDescription = productionPlan.ProductDescription,
                BuyerCompany = productionPlan.BuyerCompany,
                PlannedQuantity = productionPlan.PlannedQuantity
            };

            return View(model);
        }

        // =====================================================
        // CREATE MRP - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateMRPViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify Production Plan again on server side
            var productionPlan = await _context.ProductionPlans
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ProductionPlanId &&
                    x.ProductionStatus == "Planned");

            if (productionPlan == null)
            {
                TempData["ErrorMessage"] =
                    "Only approved/planned production plans can be used to create an MRP.";

                return RedirectToAction(nameof(ProductionPlans));
            }

            // Prevent duplicate MRP
            var existingMRP = await _context.MaterialRequirementPlans
                .AnyAsync(x =>
                    x.ProductionPlanId == model.ProductionPlanId);

            if (existingMRP)
            {
                TempData["ErrorMessage"] =
                    "An MRP already exists for this production plan.";

                return RedirectToAction(nameof(ProductionPlans));
            }

            var mrp = new MaterialRequirementPlan
            {
                MRPNumber = GenerateMRPNumber(),

                ProductionPlanId = productionPlan.Id,

                ProductDescription =
                    productionPlan.ProductDescription,

                BuyerCompany =
                    productionPlan.BuyerCompany,

                PlannedQuantity =
                    productionPlan.PlannedQuantity,

                MRPStatus = "Draft",

                Remarks = model.Remarks,

                CreatedAt = DateTime.UtcNow
            };

            _context.MaterialRequirementPlans.Add(mrp);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"MRP {mrp.MRPNumber} created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = mrp.Id });
        }

        // =====================================================
        // MRP DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mrp == null)
            {
                return NotFound();
            }

            return View(mrp);
        }

        // =====================================================
        // ADD MATERIAL - GET
        // Only Draft MRP can be modified
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AddMaterial(int mrpId)
        {
            var mrp = await _context.MaterialRequirementPlans
                .FirstOrDefaultAsync(x => x.Id == mrpId);

            if (mrp == null)
            {
                return NotFound();
            }

            if (mrp.MRPStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Materials can only be added to a Draft MRP.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = mrpId });
            }

            var model = new AddMaterialRequirementViewModel
            {
                MaterialRequirementPlanId = mrp.Id
            };

            ViewBag.MRPNumber = mrp.MRPNumber;
            ViewBag.PlannedQuantity = mrp.PlannedQuantity;

            return View(model);
        }

        // =====================================================
        // ADD MATERIAL - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial(
            AddMaterialRequirementViewModel model)
        {
            var mrp = await _context.MaterialRequirementPlans
                .FirstOrDefaultAsync(x =>
                    x.Id == model.MaterialRequirementPlanId);

            if (mrp == null)
            {
                return NotFound();
            }

            // Only Draft MRP can be modified
            if (mrp.MRPStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Materials can only be added to a Draft MRP.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = mrp.Id });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MRPNumber = mrp.MRPNumber;
                ViewBag.PlannedQuantity = mrp.PlannedQuantity;

                return View(model);
            }

            // =================================================
            // CALCULATE REQUIRED QUANTITY
            // =================================================

            var totalBeforeWastage =
                mrp.PlannedQuantity *
                model.QuantityPerPiece;

            var wastageAmount =
                totalBeforeWastage *
                (model.WastagePercentage / 100);

            var requiredQuantity =
                totalBeforeWastage + wastageAmount;

            var item = new MaterialRequirementItem
            {
                MaterialRequirementPlanId =
                    mrp.Id,

                MaterialName =
                    model.MaterialName,

                MaterialCategory =
                    model.MaterialCategory,

                Unit =
                    model.Unit,

                QuantityPerPiece =
                    model.QuantityPerPiece,

                WastagePercentage =
                    model.WastagePercentage,

                RequiredQuantity =
                    requiredQuantity,

                Remarks =
                    model.Remarks
            };

            _context.MaterialRequirementItems.Add(item);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{item.MaterialName} added to MRP.";

            return RedirectToAction(
                nameof(Details),
                new { id = mrp.Id });
        }

        // =====================================================
        // SUBMIT MRP
        // Operations Manager submits to Admin
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mrp == null)
            {
                return NotFound();
            }

            // Only Draft MRP can be submitted
            if (mrp.MRPStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft MRP can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // At least one material required
            if (!mrp.MaterialItems.Any())
            {
                TempData["ErrorMessage"] =
                    "Add at least one material before submitting MRP.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            mrp.MRPStatus = "Submitted";
            mrp.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"MRP {mrp.MRPNumber} submitted for Admin approval.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =====================================================
        // GENERATE MRP NUMBER
        // =====================================================

        private static string GenerateMRPNumber()
        {
            return $"MRP-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}