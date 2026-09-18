using System.Security.Claims;
using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager")]
    public class PackingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PackingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // CURRENT LOGGED-IN USER
        // =========================================================

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // =========================================================
        // GET USER DISPLAY NAME
        // =========================================================

        private async Task<string?> GetUserDisplayName(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return userId;

            return !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : user.UserName ?? userId;
        }

        // =========================================================
        // PACKING DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stocks = await _context.FinishedGoodsStocks
                .Include(x => x.SalesOrder)
                .Where(x =>
                    x.SalesOrderId != null &&
                    x.AvailableQuantity > 0)
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            var result = new List<PackingDashboardViewModel>();

            foreach (var stock in stocks)
            {
                // -------------------------------------------------
                // Already packed quantity
                // -------------------------------------------------

                var alreadyPacked =
                    await _context.PackingItems
                        .Where(x => x.FinishedGoodsStockId == stock.Id)
                        .SumAsync(x => (decimal?)x.Quantity) ?? 0;

                // -------------------------------------------------
                // Remaining order quantity
                // -------------------------------------------------

                var remainingOrder = Math.Max(
                    0,
                    stock.OrderedQuantity - alreadyPacked);

                // -------------------------------------------------
                // Quantity currently available for packing
                // -------------------------------------------------

                var availableForPacking = Math.Min(
                    stock.AvailableQuantity,
                    remainingOrder);

                // -------------------------------------------------
                // Pending buyer approval request
                // -------------------------------------------------

                var pendingRequest =
                    await _context.PartialShipmentRequests
                        .Where(x =>
                            x.FinishedGoodsStockId == stock.Id &&
                            x.Status == "Pending")
                        .OrderByDescending(x => x.RequestedAt)
                        .FirstOrDefaultAsync();

                // -------------------------------------------------
                // Approved buyer request
                // -------------------------------------------------

                var approvedRequest =
                    await _context.PartialShipmentRequests
                        .Where(x =>
                            x.FinishedGoodsStockId == stock.Id &&
                            x.Status == "Approved")
                        .OrderByDescending(x => x.RespondedAt)
                        .FirstOrDefaultAsync();

                // -------------------------------------------------
                // Dashboard model
                // -------------------------------------------------

                result.Add(new PackingDashboardViewModel
                {
                    FinishedGoodsStockId = stock.Id,

                    SalesOrderId = stock.SalesOrderId,

                    SalesOrderNumber =
                        stock.SalesOrder?.OrderNumber ?? "N/A",

                    ProductName = stock.ProductName,

                    Unit = stock.Unit,

                    OrderedQuantity = stock.OrderedQuantity,

                    AvailableQuantity = stock.AvailableQuantity,

                    AlreadyPackedQuantity = alreadyPacked,

                    RemainingOrderQuantity = remainingOrder,

                    QuantityAvailableForPacking = availableForPacking,

                    IsFullyAvailable =
                        remainingOrder > 0 &&
                        availableForPacking >= remainingOrder,

                    IsPartiallyAvailable =
                        availableForPacking > 0 &&
                        availableForPacking < remainingOrder,

                    IsWaitingForStock =
                        availableForPacking <= 0,

                    PartialShipmentRequestId =
                        pendingRequest?.Id ??
                        approvedRequest?.Id,

                    PartialShipmentStatus =
                        pendingRequest != null
                            ? "Pending"
                            : approvedRequest != null
                                ? "Approved"
                                : null
                });
            }

            return View(result);
        }

        // =========================================================
        // REQUEST BUYER APPROVAL FOR PARTIAL SHIPMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPartialShipment(int id)
        {
            var stock = await _context.FinishedGoodsStocks
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null)
                return NotFound();

            // -----------------------------------------------------
            // Buyer validation
            // -----------------------------------------------------

            if (stock.SalesOrder == null ||
                string.IsNullOrWhiteSpace(stock.SalesOrder.BuyerId))
            {
                TempData["Error"] =
                    "Buyer information is not available for this sales order.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Already packed
            // -----------------------------------------------------

            var alreadyPacked =
                await _context.PackingItems
                    .Where(x => x.FinishedGoodsStockId == stock.Id)
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            // -----------------------------------------------------
            // Remaining order
            // -----------------------------------------------------

            var remainingOrder = Math.Max(
                0,
                stock.OrderedQuantity - alreadyPacked);

            // -----------------------------------------------------
            // Quantity available now
            // -----------------------------------------------------

            var quantityToPack = Math.Min(
                stock.AvailableQuantity,
                remainingOrder);

            if (quantityToPack <= 0)
            {
                TempData["Error"] =
                    "No finished goods are currently available for partial shipment.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // If full order is available, approval not required
            // -----------------------------------------------------

            if (quantityToPack >= remainingOrder)
            {
                TempData["Info"] =
                    "The full remaining order quantity is available. " +
                    "Partial shipment approval is not required.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Prevent duplicate pending request
            // -----------------------------------------------------

            var existingPending =
                await _context.PartialShipmentRequests
                    .FirstOrDefaultAsync(x =>
                        x.FinishedGoodsStockId == stock.Id &&
                        x.Status == "Pending");

            if (existingPending != null)
            {
                TempData["Info"] =
                    "A partial shipment approval request is already pending with the buyer.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Create partial shipment request
            // -----------------------------------------------------

            var request = new PartialShipmentRequest
            {
                SalesOrderId = stock.SalesOrder.Id,

                FinishedGoodsStockId = stock.Id,

                RequestedQuantity = quantityToPack,

                RemainingOrderQuantity = remainingOrder,

                BuyerId = stock.SalesOrder.BuyerId,

                Status = "Pending",

                RequestedAt = DateTime.UtcNow
            };

            _context.PartialShipmentRequests.Add(request);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // Create buyer notification
            // -----------------------------------------------------

            var notification = new Notification
            {
                RecipientUserId = stock.SalesOrder.BuyerId,

                Title = "Partial Shipment Approval Required",

                Message =
                    $"Sales Order {stock.SalesOrder.OrderNumber} has " +
                    $"{quantityToPack:0.##} {stock.Unit} finished goods available " +
                    $"out of {remainingOrder:0.##} {stock.Unit} remaining. " +
                    "Please review and approve or decline the partial shipment request.",

                Type = "PartialShipment",

                IsRead = false,

                CreatedAt = DateTime.UtcNow,

                PartialShipmentRequestId = request.Id
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Partial shipment request sent to buyer for " +
                $"{quantityToPack:0.##} {stock.Unit}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // READY TO PACK - FULL SHIPMENT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ReadyToPack(int id)
        {
            var stock = await _context.FinishedGoodsStocks
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null)
                return NotFound();

            // -----------------------------------------------------
            // Already packed
            // -----------------------------------------------------

            var alreadyPacked =
                await _context.PackingItems
                    .Where(x => x.FinishedGoodsStockId == stock.Id)
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            // -----------------------------------------------------
            // Remaining order
            // -----------------------------------------------------

            var remainingOrder = Math.Max(
                0,
                stock.OrderedQuantity - alreadyPacked);

            // -----------------------------------------------------
            // Available quantity
            // -----------------------------------------------------

            var availableForPacking = Math.Min(
                stock.AvailableQuantity,
                remainingOrder);

            if (availableForPacking <= 0)
            {
                TempData["Error"] =
                    "No quantity is available for packing.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Partial shipment requires buyer approval
            // -----------------------------------------------------

            if (availableForPacking < remainingOrder)
            {
                TempData["Error"] =
                    "Buyer approval is required before proceeding with a partial shipment.";

                return RedirectToAction(nameof(Index));
            }

            var model = BuildPackingModel(
                stock,
                alreadyPacked,
                remainingOrder,
                availableForPacking,
                "FullShipment",
                null);

            return View(model);
        }

        // =========================================================
        // READY TO PACK - APPROVED PARTIAL SHIPMENT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ReadyToPackPartial(int requestId)
        {
            var request =
                await _context.PartialShipmentRequests
                    .Include(x => x.SalesOrder)
                    .Include(x => x.FinishedGoodsStock)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return NotFound();

            // -----------------------------------------------------
            // Buyer must approve first
            // -----------------------------------------------------

            if (request.Status != "Approved")
            {
                TempData["Error"] =
                    "The buyer has not approved this partial shipment request.";

                return RedirectToAction(nameof(Index));
            }

            var stock = request.FinishedGoodsStock;

            // -----------------------------------------------------
            // Already packed
            // -----------------------------------------------------

            var alreadyPacked =
                await _context.PackingItems
                    .Where(x => x.FinishedGoodsStockId == stock.Id)
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            // -----------------------------------------------------
            // Remaining order
            // -----------------------------------------------------

            var remainingOrder = Math.Max(
                0,
                stock.OrderedQuantity - alreadyPacked);

            // -----------------------------------------------------
            // Available now
            // -----------------------------------------------------

            var availableForPacking = Math.Min(
                stock.AvailableQuantity,
                remainingOrder);

            if (availableForPacking <= 0)
            {
                TempData["Error"] =
                    "No quantity is available for packing.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Do not exceed approved quantity
            // -----------------------------------------------------

            var quantityToPack = Math.Min(
                request.RequestedQuantity,
                availableForPacking);

            if (quantityToPack <= 0)
            {
                TempData["Error"] =
                    "The approved partial shipment quantity is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            var model = BuildPackingModel(
                stock,
                alreadyPacked,
                remainingOrder,
                quantityToPack,
                "PartialShipment",
                request);

            // -----------------------------------------------------
            // Show buyer name
            // -----------------------------------------------------

            model.ApprovedBy =
                await GetUserDisplayName(request.BuyerId);

            return View("ReadyToPack", model);
        }

        // =========================================================
        // BUILD PACKING MODEL
        // =========================================================

        private static CreatePackingViewModel BuildPackingModel(
            FinishedGoodsStock stock,
            decimal alreadyPacked,
            decimal remainingOrder,
            decimal quantityToPack,
            string shipmentType,
            PartialShipmentRequest? request)
        {
            return new CreatePackingViewModel
            {
                FinishedGoodsStockId = stock.Id,

                SalesOrderId = stock.SalesOrderId,

                SalesOrderNumber =
                    stock.SalesOrder?.OrderNumber ?? "N/A",

                ProductName = stock.ProductName,

                Unit = stock.Unit,

                OrderedQuantity = stock.OrderedQuantity,

                AvailableQuantity = stock.AvailableQuantity,

                AlreadyPackedQuantity = alreadyPacked,

                RemainingOrderQuantity = remainingOrder,

                QuantityAvailableForPacking = quantityToPack,

                PackingDate = DateTime.Today,

                PackingQuantity = quantityToPack,

                TotalCartons = 1,

                QuantityPerCarton = quantityToPack,

                ShipmentType = shipmentType,

                PartialShipmentApproved =
                    request != null,

                ApprovedBy = null,

                ApprovedAt = request?.RespondedAt,

                PartialShipmentRequestId =
                    request?.Id
            };
        }

        // =========================================================
        // CREATE PACKING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreatePackingViewModel model)
        {
            // -----------------------------------------------------
            // Initial validation
            // -----------------------------------------------------

            if (!ModelState.IsValid)
                return View("ReadyToPack", model);

            // -----------------------------------------------------
            // Load stock
            // -----------------------------------------------------

            var stock = await _context.FinishedGoodsStocks
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(
                    x => x.Id == model.FinishedGoodsStockId);

            if (stock == null)
                return NotFound();

            // -----------------------------------------------------
            // Recalculate server-side
            // -----------------------------------------------------

            var alreadyPacked =
                await _context.PackingItems
                    .Where(x =>
                        x.FinishedGoodsStockId == stock.Id)
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0;

            var remainingOrder = Math.Max(
                0,
                stock.OrderedQuantity - alreadyPacked);

            // -----------------------------------------------------
            // Packing quantity validation
            // -----------------------------------------------------

            if (model.PackingQuantity <= 0)
            {
                ModelState.AddModelError(
                    "PackingQuantity",
                    "Packing quantity must be greater than zero.");
            }

            if (model.PackingQuantity > stock.AvailableQuantity)
            {
                ModelState.AddModelError(
                    "PackingQuantity",
                    $"Packing quantity cannot exceed available quantity of " +
                    $"{stock.AvailableQuantity} {stock.Unit}.");
            }

            if (model.PackingQuantity > remainingOrder)
            {
                ModelState.AddModelError(
                    "PackingQuantity",
                    $"Packing quantity cannot exceed remaining order quantity of " +
                    $"{remainingOrder} {stock.Unit}.");
            }

            // -----------------------------------------------------
            // Partial shipment validation
            // -----------------------------------------------------

            PartialShipmentRequest? request = null;

            if (model.ShipmentType == "PartialShipment")
            {
                if (model.PartialShipmentRequestId == null)
                {
                    ModelState.AddModelError(
                        "",
                        "A buyer-approved partial shipment request is required.");
                }
                else
                {
                    request =
                        await _context.PartialShipmentRequests
                            .FirstOrDefaultAsync(x =>
                                x.Id ==
                                model.PartialShipmentRequestId &&

                                x.FinishedGoodsStockId ==
                                stock.Id &&

                                x.SalesOrderId ==
                                stock.SalesOrderId &&

                                x.Status == "Approved");

                    if (request == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "The buyer-approved partial shipment request " +
                            "could not be verified.");
                    }
                    else if (
                        model.PackingQuantity >
                        request.RequestedQuantity)
                    {
                        ModelState.AddModelError(
                            "PackingQuantity",
                            $"Packing quantity cannot exceed the buyer-approved quantity " +
                            $"of {request.RequestedQuantity} {stock.Unit}.");
                    }
                }
            }

            // -----------------------------------------------------
            // Carton validation
            // -----------------------------------------------------

            if (model.TotalCartons <= 0)
            {
                ModelState.AddModelError(
                    "TotalCartons",
                    "Total cartons must be greater than zero.");
            }

            if (model.QuantityPerCarton <= 0)
            {
                ModelState.AddModelError(
                    "QuantityPerCarton",
                    "Quantity per carton must be greater than zero.");
            }

            // -----------------------------------------------------
            // Return if validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                PopulatePackingModel(
                    model,
                    stock,
                    alreadyPacked,
                    remainingOrder);

                return View("ReadyToPack", model);
            }

            // -----------------------------------------------------
            // Create packing
            // -----------------------------------------------------

            var now = DateTime.UtcNow;

            var packingNumber =
                $"PK-{now:yyyyMMddHHmmssfff}";

            string? buyerApprovedBy = null;

            if (request != null)
            {
                buyerApprovedBy =
                    await GetUserDisplayName(request.BuyerId);
            }

            var packing = new Packing
            {
                PackingNumber = packingNumber,

                SalesOrderId = stock.SalesOrderId,

                PackingDate = model.PackingDate,

                TotalQuantity = model.PackingQuantity,

                TotalCartons = model.TotalCartons,

                Status = "Packed",

                ShipmentType = model.ShipmentType,

                PartialShipmentApproved =
                    model.ShipmentType == "PartialShipment",

                ApprovedBy = buyerApprovedBy,

                ApprovedAt = request?.RespondedAt,

                PartialShipmentRequestId =
                    request?.Id,

                PackedBy = CurrentUser(),

                PackedAt = now,

                Remarks = model.Remarks,

                CreatedAt = now
            };

            _context.Packings.Add(packing);

            // -----------------------------------------------------
            // Packing item
            // -----------------------------------------------------

            var packingItem = new PackingItem
            {
                Packing = packing,

                FinishedGoodsStockId = stock.Id,

                ProductName = stock.ProductName,

                Unit = stock.Unit,

                Quantity = model.PackingQuantity,

                CartonNumber = model.TotalCartons,

                QuantityPerCarton =
                    model.QuantityPerCarton,

                Remarks = model.Remarks
            };

            _context.PackingItems.Add(packingItem);

            // -----------------------------------------------------
            // Deduct finished goods stock
            // -----------------------------------------------------

            stock.AvailableQuantity -=
                model.PackingQuantity;

            stock.UpdatedAt = now;

            // -----------------------------------------------------
            // Finished Goods transaction
            // -----------------------------------------------------

            var transaction =
                new FinishedGoodsTransaction
                {
                    FinishedGoodsStockId =
                        stock.Id,

                    TransactionType =
                        "PACKING",

                    ReferenceNumber =
                        packingNumber,

                    QuantityIn = 0,

                    QuantityOut =
                        model.PackingQuantity,

                    BalanceAfter =
                        stock.AvailableQuantity,

                    TransactionDate = now,

                    PerformedBy =
                        CurrentUser(),

                    Remarks =
                        $"Finished goods packed. " +
                        $"Packing No: {packingNumber}."
                };

            _context.FinishedGoodsTransactions
                .Add(transaction);

            // -----------------------------------------------------
            // Complete partial shipment request
            // -----------------------------------------------------

            if (request != null)
            {
                request.Status = "Completed";

                request.RespondedAt ??= now;
            }

            // -----------------------------------------------------
            // Save everything
            // -----------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Packing completed successfully. " +
                $"Packing No: {packingNumber}.";

            return RedirectToAction(
                nameof(Details),
                new { id = packing.Id });
        }

        // =========================================================
        // REPULATE MODEL AFTER VALIDATION ERROR
        // =========================================================

        private static void PopulatePackingModel(
            CreatePackingViewModel model,
            FinishedGoodsStock stock,
            decimal alreadyPacked,
            decimal remainingOrder)
        {
            model.SalesOrderId =
                stock.SalesOrderId;

            model.SalesOrderNumber =
                stock.SalesOrder?.OrderNumber ?? "N/A";

            model.ProductName =
                stock.ProductName;

            model.Unit =
                stock.Unit;

            model.OrderedQuantity =
                stock.OrderedQuantity;

            model.AvailableQuantity =
                stock.AvailableQuantity;

            model.AlreadyPackedQuantity =
                alreadyPacked;

            model.RemainingOrderQuantity =
                remainingOrder;

            model.QuantityAvailableForPacking =
                Math.Min(
                    stock.AvailableQuantity,
                    remainingOrder);
        }

        // =========================================================
        // PACKING DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var packing =
                await _context.Packings

                    .Include(x => x.SalesOrder)

                    .Include(x => x.Items)
                        .ThenInclude(x =>
                            x.FinishedGoodsStock)

                    .Include(x => x.Shipments)

                    .Include(x =>
                        x.PartialShipmentRequest)

                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (packing == null)
                return NotFound();

            return View(packing);
        }
    }
}