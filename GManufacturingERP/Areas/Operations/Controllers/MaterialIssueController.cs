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
    public class MaterialIssueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaterialIssueController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Operations/MaterialIssue
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var issues = await _context.MaterialIssues
                .Include(x => x.Items)
                .Include(x => x.MaterialRequirementPlan)
                .OrderByDescending(x => x.IssueDate)
                .ToListAsync();

            return View(issues);
        }

        // GET: Operations/MaterialIssue/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateMaterialIssueViewModel();

            await LoadApprovedMRPs(model);

            return View(model);
        }

        // GET: Operations/MaterialIssue/CreateFromMRP/1
        [HttpGet]
        public async Task<IActionResult> CreateFromMRP(
            int materialRequirementPlanId)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == materialRequirementPlanId &&
                    x.MRPStatus == "Approved");

            if (mrp == null)
            {
                TempData["Error"] =
                    "Approved MRP was not found.";

                return RedirectToAction(nameof(Create));
            }

            var alreadyExists = await _context.MaterialIssues
                .AnyAsync(x =>
                    x.MaterialRequirementPlanId ==
                    materialRequirementPlanId &&
                    x.Status != "Rejected");

            if (alreadyExists)
            {
                TempData["Error"] =
                    "A material issue already exists for this MRP.";

                return RedirectToAction(nameof(Create));
            }

            var model = await BuildMaterialIssueModel(mrp);

            await LoadApprovedMRPs(model);

            return View("Create", model);
        }

        // POST: Operations/MaterialIssue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateMaterialIssueViewModel model)
        {
            var mrp = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Include(x => x.MaterialItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.MaterialRequirementPlanId &&
                    x.MRPStatus == "Approved");

            if (mrp == null)
            {
                ModelState.AddModelError(
                    nameof(model.MaterialRequirementPlanId),
                    "Please select a valid approved MRP.");

                await LoadApprovedMRPs(model);

                return View(model);
            }

            var alreadyExists = await _context.MaterialIssues
                .AnyAsync(x =>
                    x.MaterialRequirementPlanId == mrp.Id &&
                    x.Status != "Rejected");

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    "",
                    "A material issue already exists for this MRP.");

                await LoadApprovedMRPs(model);

                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await LoadApprovedMRPs(model);

                return View(model);
            }

            var issue = new MaterialIssue
            {
                IssueNumber = GenerateIssueNumber(),

                MaterialRequirementPlanId = mrp.Id,

                ProductionReference =
                    mrp.ProductionPlan?.PlanNumber
                    ?? $"MRP-{mrp.MRPNumber}",

                IssueDate = DateTime.UtcNow,

                // Important:
                // Operations only creates the request.
                // Admin will approve it later.
                Status = "Draft",

                RequestedBy =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? User.Identity?.Name
                    ?? "System",

                Remarks = model.Remarks
            };

            foreach (var mrpItem in mrp.MaterialItems)
            {
                // IMPORTANT:
                // Match MaterialName + Category + Unit
                // to avoid selecting wrong duplicate stock.
                var materialName =
                    (mrpItem.MaterialName ?? string.Empty).Trim();

                var materialCategory =
                    (mrpItem.MaterialCategory ?? string.Empty).Trim();

                var unit =
                    (mrpItem.Unit ?? string.Empty).Trim();

                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(x =>
                        x.MaterialName == materialName &&
                        x.MaterialCategory == materialCategory &&
                        x.Unit == unit);

                if (stock == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Inventory stock not found for " +
                        $"{materialName} ({materialCategory}, {unit}).");

                    await LoadApprovedMRPs(model);

                    return View(model);
                }

                var postedItem = model.Items?
                    .FirstOrDefault(x =>
                        x.MaterialName == mrpItem.MaterialName &&
                        x.MaterialCategory == mrpItem.MaterialCategory &&
                        x.Unit == mrpItem.Unit);

                var issueQuantity =
                    postedItem?.IssuedQuantity
                    ?? mrpItem.RequiredQuantity;

                if (issueQuantity <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"Issue quantity must be greater than zero for " +
                        $"{materialName}.");

                    await LoadApprovedMRPs(model);

                    return View(model);
                }

                if (issueQuantity > stock.AvailableQuantity)
                {
                    ModelState.AddModelError(
                        "",
                        $"Insufficient stock for {stock.MaterialName}. " +
                        $"Available: {stock.AvailableQuantity} {stock.Unit}.");

                    await LoadApprovedMRPs(model);

                    return View(model);
                }

                issue.Items.Add(new MaterialIssueItem
                {
                    InventoryStockId = stock.Id,

                    MaterialName = materialName,

                    MaterialCategory = materialCategory,

                    Unit = unit,

                    RequestedQuantity = issueQuantity,

                    // Actual inventory deduction happens
                    // only after Admin approval.
                    IssuedQuantity = 0
                });
            }

            _context.MaterialIssues.Add(issue);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Material issue request created successfully. " +
                "Please submit it for Admin approval.";

            return RedirectToAction(
                nameof(Details),
                new { id = issue.Id });
        }

        // GET: Operations/MaterialIssue/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.MaterialIssues
                .Include(x => x.Items)
                .Include(x => x.MaterialRequirementPlan)
                    .ThenInclude(x => x!.ProductionPlan)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            return View(issue);
        }

        // POST: Operations/MaterialIssue/Submit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var issue = await _context.MaterialIssues
                .FirstOrDefaultAsync(x => x.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            if (issue.Status != "Draft")
            {
                TempData["Error"] =
                    "Only draft issues can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            issue.Status = "Submitted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Material issue submitted successfully " +
                "and sent to Admin for approval.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // Load only approved and unused MRPs
        private async Task LoadApprovedMRPs(
            CreateMaterialIssueViewModel model)
        {
            var usedMRPIds = await _context.MaterialIssues
                .Where(x =>
                    x.MaterialRequirementPlanId != null &&
                    x.Status != "Rejected")
                .Select(x =>
                    x.MaterialRequirementPlanId!.Value)
                .ToListAsync();

            var mrps = await _context.MaterialRequirementPlans
                .Include(x => x.ProductionPlan)
                .Where(x =>
                    x.MRPStatus == "Approved" &&
                    !usedMRPIds.Contains(x.Id))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            model.ApprovedMRPs = mrps
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),

                    Text =
                        $"{x.MRPNumber} | " +
                        $"{x.ProductDescription} | " +
                        $"Qty: {x.PlannedQuantity}",

                    Selected =
                        model.MaterialRequirementPlanId == x.Id
                })
                .ToList();
        }

        // Build Material Issue form from approved MRP
        private async Task<CreateMaterialIssueViewModel>
            BuildMaterialIssueModel(
                MaterialRequirementPlan mrp)
        {
            var model = new CreateMaterialIssueViewModel
            {
                MaterialRequirementPlanId = mrp.Id,

                ProductionReference =
                    mrp.ProductionPlan?.PlanNumber
                    ?? mrp.MRPNumber,

                ProductName = mrp.ProductDescription,

                PlannedQuantity = mrp.PlannedQuantity,

                Unit = "Pieces",

                Remarks = mrp.Remarks
            };

            foreach (var mrpItem in mrp.MaterialItems)
            {
                var materialName =
                    (mrpItem.MaterialName ?? string.Empty).Trim();

                var materialCategory =
                    (mrpItem.MaterialCategory ?? string.Empty).Trim();

                var unit =
                    (mrpItem.Unit ?? string.Empty).Trim();

                // IMPORTANT:
                // Match all 3 fields.
                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(x =>
                        x.MaterialName == materialName &&
                        x.MaterialCategory == materialCategory &&
                        x.Unit == unit);

                model.Items.Add(
                    new CreateMaterialIssueItemViewModel
                    {
                        InventoryStockId =
                            stock?.Id ?? 0,

                        MaterialName =
                            materialName,

                        MaterialCategory =
                            materialCategory,

                        Unit =
                            unit,

                        RequiredQuantity =
                            mrpItem.RequiredQuantity,

                        AvailableQuantity =
                            stock?.AvailableQuantity ?? 0,

                        IssuedQuantity =
                            mrpItem.RequiredQuantity
                    });
            }

            return model;
        }

        private static string GenerateIssueNumber()
        {
            return $"MI-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}