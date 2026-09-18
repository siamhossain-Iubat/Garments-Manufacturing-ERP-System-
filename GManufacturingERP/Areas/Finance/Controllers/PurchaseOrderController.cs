using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager")]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // -----------------------------------------------------
            // Existing Purchase Orders
            // -----------------------------------------------------

            var purchaseOrders =
                await _context.PurchaseOrders
                    .Include(x => x.PurchaseRequisition)
                    .Include(x => x.Items)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();


            // -----------------------------------------------------
            // Approved Purchase Requisitions
            // Which do NOT have a Purchase Order yet
            // -----------------------------------------------------

            var approvedRequisitions =
                await _context.PurchaseRequisitions
                    .Include(x => x.Items)
                    .Where(x =>
                        x.RequisitionStatus == "Approved" &&
                        !_context.PurchaseOrders
                            .Any(po =>
                                po.PurchaseRequisitionId == x.Id))
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();


            // Send approved PRs to the View

            ViewBag.ApprovedRequisitions =
                approvedRequisitions;


            return View(purchaseOrders);
        }


        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var purchaseOrder =
                await _context.PurchaseOrders
                    .Include(x => x.PurchaseRequisition)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (purchaseOrder == null)
            {
                return NotFound();
            }


            return View(purchaseOrder);
        }


        // =========================================================
        // CREATE PURCHASE ORDER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int requisitionId)
        {
            // -----------------------------------------------------
            // Find only APPROVED Purchase Requisition
            // -----------------------------------------------------

            var requisition =
                await _context.PurchaseRequisitions
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == requisitionId &&
                        x.RequisitionStatus == "Approved");


            if (requisition == null)
            {
                TempData["ErrorMessage"] =
                    "Only approved requisitions can create a Purchase Order.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Prevent duplicate Purchase Order
            // -----------------------------------------------------

            var existingPO =
                await _context.PurchaseOrders
                    .AnyAsync(x =>
                        x.PurchaseRequisitionId ==
                        requisitionId);


            if (existingPO)
            {
                TempData["ErrorMessage"] =
                    "A Purchase Order already exists for this requisition.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Prepare PO Create Model
            // -----------------------------------------------------

            var model =
                new CreatePurchaseOrderViewModel
                {
                    PurchaseRequisitionId =
                        requisition.Id,

                    RequisitionNumber =
                        requisition.RequisitionNumber,

                    OrderDate =
                        DateTime.Today,

                    ExpectedDeliveryDate =
                        DateTime.Today.AddDays(15),

                    PaymentTerms =
                        "30 Days",

                    DeliveryTerms =
                        "Factory Delivery",

                    Items =
                        requisition.Items
                            .Select(x =>
                                new CreatePurchaseOrderItemViewModel
                                {
                                    MaterialName =
                                        x.MaterialName,

                                    MaterialCategory =
                                        x.MaterialCategory,

                                    Unit =
                                        x.Unit,

                                    OrderedQuantity =
                                        x.RequestedQuantity,

                                    UnitPrice =
                                        x.EstimatedUnitPrice,

                                    Remarks =
                                        x.Remarks
                                })
                            .ToList()
                };


            return View(model);
        }


        // =========================================================
        // CREATE PURCHASE ORDER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreatePurchaseOrderViewModel model)
        {
            // -----------------------------------------------------
            // Server-side validation
            // PR must still be APPROVED
            // -----------------------------------------------------

            var requisition =
                await _context.PurchaseRequisitions
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.PurchaseRequisitionId &&
                        x.RequisitionStatus == "Approved");


            if (requisition == null)
            {
                TempData["ErrorMessage"] =
                    "Only approved requisitions can create a Purchase Order.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Prevent duplicate PO
            // -----------------------------------------------------

            var existingPO =
                await _context.PurchaseOrders
                    .AnyAsync(x =>
                        x.PurchaseRequisitionId ==
                        model.PurchaseRequisitionId);


            if (existingPO)
            {
                TempData["ErrorMessage"] =
                    "A Purchase Order already exists for this requisition.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Validate Model
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // -----------------------------------------------------
            // Create Purchase Order
            // -----------------------------------------------------

            var purchaseOrder =
                new PurchaseOrder
                {
                    PurchaseOrderNumber =
                        GeneratePurchaseOrderNumber(),

                    PurchaseRequisitionId =
                        model.PurchaseRequisitionId,

                    SupplierName =
                        model.SupplierName,

                    SupplierContact =
                        model.SupplierContact,

                    SupplierAddress =
                        model.SupplierAddress,

                    OrderDate =
                        model.OrderDate,

                    ExpectedDeliveryDate =
                        model.ExpectedDeliveryDate,

                    PaymentTerms =
                        model.PaymentTerms ?? "30 Days",

                    DeliveryTerms =
                        model.DeliveryTerms ?? "Factory Delivery",

                    PurchaseOrderStatus =
                        "Draft",

                    Remarks =
                        model.Remarks,

                    CreatedAt =
                        DateTime.UtcNow,

                    SubTotal =
                        model.Items.Sum(x =>
                            x.OrderedQuantity *
                            x.UnitPrice),

                    TaxAmount =
                        0,

                    Items =
                        model.Items
                            .Select(x =>
                                new PurchaseOrderItem
                                {
                                    MaterialName =
                                        x.MaterialName,

                                    MaterialCategory =
                                        x.MaterialCategory,

                                    Unit =
                                        x.Unit,

                                    OrderedQuantity =
                                        x.OrderedQuantity,

                                    UnitPrice =
                                        x.UnitPrice,

                                    TotalPrice =
                                        x.OrderedQuantity *
                                        x.UnitPrice,

                                    Remarks =
                                        x.Remarks
                                })
                            .ToList()
                };


            // -----------------------------------------------------
            // Calculate Grand Total
            // -----------------------------------------------------

            purchaseOrder.GrandTotal =
                purchaseOrder.SubTotal +
                purchaseOrder.TaxAmount;


            // -----------------------------------------------------
            // Save Purchase Order
            // -----------------------------------------------------

            _context.PurchaseOrders.Add(
                purchaseOrder);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"Purchase Order {purchaseOrder.PurchaseOrderNumber} created successfully.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = purchaseOrder.Id
                });
        }


        // =========================================================
        // SUBMIT PURCHASE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var purchaseOrder =
                await _context.PurchaseOrders
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (purchaseOrder == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Only Draft PO can be submitted
            // -----------------------------------------------------

            if (purchaseOrder.PurchaseOrderStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft Purchase Orders can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    });
            }


            // -----------------------------------------------------
            // Draft → Submitted
            // -----------------------------------------------------

            purchaseOrder.PurchaseOrderStatus =
                "Submitted";


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Purchase Order submitted for Admin approval.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }


        // =========================================================
        // PURCHASE ORDER NUMBER GENERATOR
        // =========================================================

        private static string GeneratePurchaseOrderNumber()
        {
            return $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}