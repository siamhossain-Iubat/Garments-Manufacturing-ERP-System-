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
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ALL PRODUCTION PLANS
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var plans = await _context.ProductionPlans
                .Include(x => x.SalesOrder)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(plans);
        }

        // =========================================================
        // APPROVED SALES ORDERS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ApprovedOrders()
        {
            var orders = await _context.SalesOrders
                .Where(x => x.OrderStatus == "Approved")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // =========================================================
        // CREATE PRODUCTION PLAN - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create(int salesOrderId)
        {
            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x =>
                    x.Id == salesOrderId &&
                    x.OrderStatus == "Approved");

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "Only approved sales orders can be planned.";

                return RedirectToAction(nameof(ApprovedOrders));
            }

            // One production plan per sales order
            var existingPlan = await _context.ProductionPlans
                .AnyAsync(x => x.SalesOrderId == salesOrderId);

            if (existingPlan)
            {
                TempData["ErrorMessage"] =
                    "A production plan already exists for this order.";

                return RedirectToAction(nameof(ApprovedOrders));
            }

            var model = new CreateProductionPlanViewModel
            {
                SalesOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ProductDescription = order.ProductDescription,
                BuyerCompany = order.CompanyName,
                OrderedQuantity = order.Quantity,
                PlannedQuantity = order.Quantity,
                PlannedStartDate = DateTime.Today,
                ExpectedCompletionDate = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        // =========================================================
        // CREATE PRODUCTION PLAN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateProductionPlanViewModel model)
        {
            // Date validation
            if (model.ExpectedCompletionDate <
                model.PlannedStartDate)
            {
                ModelState.AddModelError(
                    "ExpectedCompletionDate",
                    "Completion date must be after the start date.");
            }

            // Quantity validation
            if (model.PlannedQuantity > model.OrderedQuantity)
            {
                ModelState.AddModelError(
                    "PlannedQuantity",
                    "Planned quantity cannot exceed ordered quantity.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify that the sales order is still approved
            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SalesOrderId &&
                    x.OrderStatus == "Approved");

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "The selected sales order is no longer available.";

                return RedirectToAction(nameof(ApprovedOrders));
            }

            // Prevent duplicate production plan
            var existingPlan = await _context.ProductionPlans
                .AnyAsync(x =>
                    x.SalesOrderId == model.SalesOrderId);

            if (existingPlan)
            {
                TempData["ErrorMessage"] =
                    "A production plan already exists for this order.";

                return RedirectToAction(nameof(ApprovedOrders));
            }

            // Create new production plan
            var plan = new ProductionPlan
            {
                PlanNumber = GeneratePlanNumber(),

                SalesOrderId = order.Id,

                ProductDescription = order.ProductDescription,

                BuyerCompany = order.CompanyName,

                PlannedQuantity = model.PlannedQuantity,

                PlannedStartDate = model.PlannedStartDate,

                ExpectedCompletionDate =
                    model.ExpectedCompletionDate,

                // IMPORTANT:
                // Operations Manager creates it,
                // Admin must approve it.
                ProductionStatus = "PendingApproval",

                ProductionRemarks = model.ProductionRemarks,

                CreatedAt = DateTime.UtcNow
            };

            _context.ProductionPlans.Add(plan);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Production plan {plan.PlanNumber} submitted for admin approval.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // PRODUCTION PLAN DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _context.ProductionPlans
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // =========================================================
        // PLAN NUMBER GENERATOR
        // =========================================================
        private string GeneratePlanNumber()
        {
            return $"PP-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}