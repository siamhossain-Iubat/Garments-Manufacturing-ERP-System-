using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager")]
    public class GoodsReceiptController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoodsReceiptController(ApplicationDbContext context)
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
            // Existing Goods Receipts
            // -----------------------------------------------------

            var goodsReceipts =
                await _context.GoodsReceipts
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.Items)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();


            // -----------------------------------------------------
            // Approved Purchase Orders Ready for GRN
            // -----------------------------------------------------

            var approvedPurchaseOrders =
                await _context.PurchaseOrders
                    .Where(x =>
                        x.PurchaseOrderStatus == "Approved")
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();


            // Send Approved Purchase Orders to View
            ViewBag.ApprovedPurchaseOrders =
                approvedPurchaseOrders;


            // Existing Goods Receipts remain the Model
            return View(goodsReceipts);
        }


        // =========================================================
        // SELECT PURCHASE ORDER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SelectPurchaseOrder()
        {
            var purchaseOrders =
                await _context.PurchaseOrders
                    .Where(x =>
                        x.PurchaseOrderStatus == "Approved")
                    .Include(x => x.Items)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

            return View(purchaseOrders);
        }


        // =========================================================
        // CREATE GRN - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int purchaseOrderId)
        {
            // -----------------------------------------------------
            // Find Purchase Order
            // -----------------------------------------------------

            var purchaseOrder =
                await _context.PurchaseOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == purchaseOrderId);


            // -----------------------------------------------------
            // Purchase Order Not Found
            // -----------------------------------------------------

            if (purchaseOrder == null)
            {
                TempData["ErrorMessage"] =
                    "Purchase Order not found.";

                return RedirectToAction(
                    nameof(SelectPurchaseOrder));
            }


            // -----------------------------------------------------
            // Only Approved PO can create GRN
            // -----------------------------------------------------

            if (purchaseOrder.PurchaseOrderStatus != "Approved")
            {
                TempData["ErrorMessage"] =
                    "Only approved Purchase Orders can create GRN.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // Create GRN ViewModel
            // -----------------------------------------------------

            var model =
                new CreateGoodsReceiptViewModel
                {
                    PurchaseOrderId =
                        purchaseOrder.Id,

                    PurchaseOrderNumber =
                        purchaseOrder.PurchaseOrderNumber,

                    ReceiptDate =
                        DateTime.Today,

                    Items =
                        purchaseOrder.Items
                            .Select(x =>
                                new GoodsReceiptItemViewModel
                                {
                                    MaterialName =
                                        x.MaterialName,

                                    MaterialCategory =
                                        x.MaterialCategory,

                                    Unit =
                                        x.Unit,

                                    OrderedQuantity =
                                        x.OrderedQuantity,

                                    ReceivedQuantity =
                                        0,

                                    AcceptedQuantity =
                                        0,

                                    RejectedQuantity =
                                        0,

                                    Remarks =
                                        string.Empty
                                })
                            .ToList()
                };


            return View(model);
        }


        // =========================================================
        // CREATE GRN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateGoodsReceiptViewModel model)
        {
            // -----------------------------------------------------
            // Basic Validation
            // -----------------------------------------------------

            if (model.Items == null ||
                model.Items.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "At least one material item is required.");
            }


            if (model.ReceiptDate == default)
            {
                ModelState.AddModelError(
                    nameof(model.ReceiptDate),
                    "Receipt date is required.");
            }


            // -----------------------------------------------------
            // Quantity Validation
            // -----------------------------------------------------

            if (model.Items != null)
            {
                foreach (var item in model.Items)
                {
                    // Quantity cannot be negative
                    if (item.ReceivedQuantity < 0 ||
                        item.AcceptedQuantity < 0 ||
                        item.RejectedQuantity < 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Quantity cannot be negative.");
                    }


                    // Accepted + Rejected
                    // cannot exceed Received
                    if (item.AcceptedQuantity +
                        item.RejectedQuantity >
                        item.ReceivedQuantity)
                    {
                        ModelState.AddModelError(
                            "",
                            "Accepted and rejected quantity cannot exceed received quantity.");
                    }


                    // Received quantity cannot exceed Ordered quantity
                    if (item.ReceivedQuantity >
                        item.OrderedQuantity)
                    {
                        ModelState.AddModelError(
                            "",
                            "Received quantity cannot exceed ordered quantity.");
                    }
                }
            }


            // -----------------------------------------------------
            // Model Validation
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // -----------------------------------------------------
            // Verify Purchase Order
            // -----------------------------------------------------

            var purchaseOrder =
                await _context.PurchaseOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.PurchaseOrderId);


            // Purchase Order not found
            if (purchaseOrder == null)
            {
                TempData["ErrorMessage"] =
                    "Purchase Order not found.";

                return RedirectToAction(
                    nameof(SelectPurchaseOrder));
            }


            // Only Approved PO can create GRN
            if (purchaseOrder.PurchaseOrderStatus != "Approved")
            {
                TempData["ErrorMessage"] =
                    "Only approved Purchase Orders can create GRN.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // Create Goods Receipt
            // -----------------------------------------------------

            var receipt =
                new GoodsReceipt
                {
                    GRNNumber =
                        GenerateGRNNumber(),

                    PurchaseOrderId =
                        model.PurchaseOrderId,

                    ReceivedBy =
                        User.FindFirstValue(
                            ClaimTypes.Email)
                        ??
                        User.Identity?.Name
                        ??
                        "System",

                    ReceiptDate =
                        model.ReceiptDate,

                    ReceiptStatus =
                        "Draft",

                    DeliveryChallanNumber =
                        model.DeliveryChallanNumber,

                    Remarks =
                        model.Remarks,

                    CreatedAt =
                        DateTime.UtcNow,

                    Items =
                        model.Items
                            .Select(x =>
                                new GoodsReceiptItem
                                {
                                    MaterialName =
                                        x.MaterialName,

                                    MaterialCategory =
                                        x.MaterialCategory,

                                    Unit =
                                        x.Unit,

                                    OrderedQuantity =
                                        x.OrderedQuantity,

                                    ReceivedQuantity =
                                        x.ReceivedQuantity,

                                    AcceptedQuantity =
                                        x.AcceptedQuantity,

                                    RejectedQuantity =
                                        x.RejectedQuantity,

                                    Remarks =
                                        x.Remarks
                                })
                            .ToList()
                };


            _context.GoodsReceipts.Add(
                receipt);


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"Goods Receipt {receipt.GRNNumber} created successfully.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = receipt.Id
                });
        }


        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var receipt =
                await _context.GoodsReceipts
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                TempData["ErrorMessage"] =
                    "Goods Receipt not found.";

                return RedirectToAction(
                    nameof(Index));
            }


            return View(receipt);
        }


        // =========================================================
        // SUBMIT GRN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var receipt =
                await _context.GoodsReceipts
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                TempData["ErrorMessage"] =
                    "Goods Receipt not found.";

                return RedirectToAction(
                    nameof(Index));
            }


            // Only Draft GRN can be submitted
            if (receipt.ReceiptStatus != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft GRN can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    });
            }


            receipt.ReceiptStatus =
                "Submitted";


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Goods Receipt submitted for Admin approval.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }


        // =========================================================
        // SYNC INVENTORY
        // Only Finance can perform this after Admin approval
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncInventory(int id)
        {
            var receipt =
                await _context.GoodsReceipts
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (receipt == null)
            {
                TempData["ErrorMessage"] =
                    "Goods Receipt not found.";

                return RedirectToAction(
                    nameof(Index));
            }


            // Only Approved GRN can sync inventory
            if (receipt.ReceiptStatus != "Approved")
            {
                TempData["ErrorMessage"] =
                    "Only Approved GRN can sync inventory.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    });
            }


            var performedBy =
                User.FindFirstValue(
                    ClaimTypes.Email)
                ??
                User.Identity?.Name
                ??
                "System";


            int syncedItemCount = 0;


            // -----------------------------------------------------
            // Process each GRN item
            // -----------------------------------------------------

            foreach (var receiptItem in receipt.Items)
            {
                // Ignore items with no accepted quantity
                if (receiptItem.AcceptedQuantity <= 0)
                {
                    continue;
                }


                var materialName =
                    (receiptItem.MaterialName ??
                     string.Empty).Trim();


                var materialCategory =
                    (receiptItem.MaterialCategory ??
                     string.Empty).Trim();


                var unit =
                    (receiptItem.Unit ??
                     string.Empty).Trim();


                // -------------------------------------------------
                // Prevent duplicate inventory synchronization
                // -------------------------------------------------

                var alreadySynced =
                    await _context.InventoryTransactions
                        .AnyAsync(x =>
                            x.ReferenceNumber ==
                                receipt.GRNNumber
                            &&
                            x.TransactionType ==
                                "GRN_RECEIPT"
                            &&
                            x.QuantityIn ==
                                receiptItem.AcceptedQuantity
                            &&
                            x.Remarks != null
                            &&
                            x.Remarks.Contains(
                                materialName));


                if (alreadySynced)
                {
                    continue;
                }


                // -------------------------------------------------
                // Find existing stock
                // -------------------------------------------------

                var stock =
                    await _context.InventoryStocks
                        .FirstOrDefaultAsync(x =>
                            x.MaterialName ==
                                materialName
                            &&
                            x.MaterialCategory ==
                                materialCategory
                            &&
                            x.Unit ==
                                unit);


                // -------------------------------------------------
                // Create stock if not found
                // -------------------------------------------------

                if (stock == null)
                {
                    stock =
                        new InventoryStock
                        {
                            MaterialName =
                                materialName,

                            MaterialCategory =
                                materialCategory,

                            Unit =
                                unit,

                            AvailableQuantity =
                                0,

                            ReservedQuantity =
                                0,

                            ReorderLevel =
                                0,

                            Location =
                                "Main Store",

                            UpdatedAt =
                                DateTime.UtcNow
                        };


                    _context.InventoryStocks.Add(
                        stock);


                    await _context.SaveChangesAsync();
                }


                // -------------------------------------------------
                // Update stock
                // -------------------------------------------------

                stock.AvailableQuantity +=
                    receiptItem.AcceptedQuantity;

                stock.UpdatedAt =
                    DateTime.UtcNow;


                // -------------------------------------------------
                // Create inventory transaction
                // -------------------------------------------------

                var transaction =
                    new InventoryTransaction
                    {
                        InventoryStockId =
                            stock.Id,

                        TransactionType =
                            "GRN_RECEIPT",

                        ReferenceNumber =
                            receipt.GRNNumber,

                        QuantityIn =
                            receiptItem.AcceptedQuantity,

                        QuantityOut =
                            0,

                        BalanceAfter =
                            stock.AvailableQuantity,

                        TransactionDate =
                            DateTime.UtcNow,

                        PerformedBy =
                            performedBy,

                        Remarks =
                            $"Stock received through GRN {receipt.GRNNumber} - {materialName}"
                    };


                _context.InventoryTransactions.Add(
                    transaction);


                syncedItemCount++;
            }


            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // Sync Result
            // -----------------------------------------------------

            if (syncedItemCount == 0)
            {
                TempData["ErrorMessage"] =
                    "This GRN was already synced or has no accepted quantity.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    $"Inventory synced successfully. {syncedItemCount} material item(s) added to stock.";
            }


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }


        // =========================================================
        // GRN NUMBER GENERATOR
        // =========================================================

        private static string GenerateGRNNumber()
        {
            return $"GRN-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}