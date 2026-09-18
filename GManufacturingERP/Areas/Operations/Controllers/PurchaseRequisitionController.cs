using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GManufacturingERP.Areas.Operations.Controllers
{
    [Area("Operations")]
    [Authorize(Roles = "OperationsManager")]
    public class PurchaseRequisitionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchaseRequisitionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requisitions = await _context.PurchaseRequisitions
                .Include(x => x.MaterialRequirementPlan)
                .Include(x => x.Items)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(requisitions);
        }

        // =========================================================
        // SELECT APPROVED MRP
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SelectMRP()
        {
            var mrps = await _context.MaterialRequirementPlans
                .Where(x =>
                    x.MRPStatus == "Approved" &&
                    !_context.PurchaseRequisitions
                        .Any(pr =>
                            pr.MaterialRequirementPlanId == x.Id))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(mrps);
        }

        // =========================================================
        // CREATE PR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int mrpId)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == mrpId &&
                    x.MRPStatus == "Approved");

            if (mrp == null)
            {
                TempData["ErrorMessage"] =
                    "Only an approved MRP can create a purchase requisition.";

                return RedirectToAction(nameof(SelectMRP));
            }

            var existingRequisition =
                await _context.PurchaseRequisitions
                    .AnyAsync(x =>
                        x.MaterialRequirementPlanId == mrpId);

            if (existingRequisition)
            {
                TempData["ErrorMessage"] =
                    "A purchase requisition already exists for this MRP.";

                return RedirectToAction(nameof(SelectMRP));
            }

            var model = new CreatePurchaseRequisitionViewModel
            {
                MaterialRequirementPlanId = mrp.Id,

                MRPNumber = mrp.MRPNumber,

                BuyerCompany = mrp.BuyerCompany,

                ProductDescription = mrp.ProductDescription,

                RequiredDate = DateTime.Today.AddDays(15),

                Priority = "Normal",

                Items = mrp.MaterialItems
                    .Select(x => new PurchaseRequisitionItemViewModel
                    {
                        MaterialName = x.MaterialName,
                        MaterialCategory = x.MaterialCategory,
                        Unit = x.Unit,
                        RequestedQuantity = x.RequiredQuantity,
                        EstimatedUnitPrice = 0,
                        Remarks = x.Remarks
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // CREATE PR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreatePurchaseRequisitionViewModel model)
        {
            // Server-side MRP validation
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.MaterialRequirementPlanId &&
                    x.MRPStatus == "Approved");

            if (mrp == null)
            {
                TempData["ErrorMessage"] =
                    "Only an approved MRP can create a purchase requisition.";

                return RedirectToAction(nameof(SelectMRP));
            }

            // Prevent duplicate PR
            var existingRequisition =
                await _context.PurchaseRequisitions
                    .AnyAsync(x =>
                        x.MaterialRequirementPlanId ==
                        model.MaterialRequirementPlanId);

            if (existingRequisition)
            {
                TempData["ErrorMessage"] =
                    "A purchase requisition already exists for this MRP.";

                return RedirectToAction(nameof(SelectMRP));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var requisition = new PurchaseRequisition
            {
                RequisitionNumber =
                    GenerateRequisitionNumber(),

                MaterialRequirementPlanId =
                    mrp.Id,

                RequestedBy =
                    userId
                    ?? User.Identity?.Name
                    ?? "System",

                RequiredDate =
                    model.RequiredDate,

                Priority =
                    model.Priority,

                // New PR starts as Draft
                RequisitionStatus =
                    "Draft",

                Remarks =
                    model.Remarks,

                CreatedAt =
                    DateTime.UtcNow,

                Items =
                    new List<PurchaseRequisitionItem>()
            };

            foreach (var item in model.Items)
            {
                requisition.Items.Add(
                    new PurchaseRequisitionItem
                    {
                        MaterialName =
                            item.MaterialName,

                        MaterialCategory =
                            item.MaterialCategory,

                        Unit =
                            item.Unit,

                        RequestedQuantity =
                            item.RequestedQuantity,

                        EstimatedUnitPrice =
                            item.EstimatedUnitPrice,

                        EstimatedTotalPrice =
                            item.RequestedQuantity *
                            item.EstimatedUnitPrice,

                        Remarks =
                            item.Remarks
                    });
            }

            _context.PurchaseRequisitions.Add(
                requisition);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Purchase Requisition created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = requisition.Id });
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var requisition =
                await _context.PurchaseRequisitions
                    .Include(x => x.MaterialRequirementPlan)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }

            return View(requisition);
        }

        // =========================================================
        // SUBMIT PR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var requisition =
                await _context.PurchaseRequisitions
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }

            if (requisition.RequisitionStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft requisitions can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!requisition.Items.Any())
            {
                TempData["ErrorMessage"] =
                    "A purchase requisition must contain at least one item.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            requisition.RequisitionStatus =
                "Submitted";

            requisition.SubmittedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Purchase Requisition submitted to Finance & Logistics for approval.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // NUMBER GENERATOR
        // =========================================================

        private static string GenerateRequisitionNumber()
        {
            return $"PR-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}