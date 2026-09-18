using System.Security.Claims;
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
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeliveryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CURRENT USER
        // =========================================================

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // =========================================================
        // DELIVERY LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var deliveries = await _context.Deliveries
                .Include(x => x.Dispatch)
                    .ThenInclude(x => x.Shipment)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(deliveries);
        }

        // =========================================================
        // DELIVERY DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var delivery = await _context.Deliveries
                .Include(x => x.Dispatch)
                    .ThenInclude(x => x.Shipment)
                        .ThenInclude(x => x.Packing)
                            .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (delivery == null)
                return NotFound();

            return View(delivery);
        }

        // =========================================================
        // CREATE DELIVERY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int dispatchId)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                    .ThenInclude(x => x.Packing)
                        .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == dispatchId);

            if (dispatch == null)
                return NotFound();

            // Only In Transit dispatch can create delivery
            if (dispatch.Status != "In Transit")
            {
                TempData["ErrorMessage"] =
                    "Only In Transit dispatches can be delivered.";

                return RedirectToAction(
                    "Details",
                    "Dispatch",
                    new
                    {
                        area = "Finance",
                        id = dispatchId
                    });
            }

            // Prevent duplicate delivery
            var existingDelivery =
                await _context.Deliveries
                    .FirstOrDefaultAsync(
                        x => x.DispatchId == dispatchId);

            if (existingDelivery != null)
            {
                TempData["ErrorMessage"] =
                    "A delivery record already exists for this dispatch.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = existingDelivery.Id
                    });
            }

            var model = new CreateDeliveryViewModel
            {
                DispatchId = dispatch.Id,

                DispatchNumber =
                    dispatch.DispatchNumber,

                ShipmentNumber =
                    dispatch.Shipment?.ShipmentNumber ?? "N/A",

                ProductName =
                    dispatch.ProductName,

                Unit =
                    dispatch.Unit,

                DispatchQuantity =
                    dispatch.Quantity,

                DispatchCartons =
                    dispatch.TotalCartons,

                DeliveryQuantity =
                    dispatch.Quantity,

                TotalCartons =
                    dispatch.TotalCartons,

                DeliveryDate =
                    DateTime.Today,

                DeliveryAddress =
                    dispatch.Destination
            };

            return View(model);
        }

        // =========================================================
        // CREATE DELIVERY - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateDeliveryViewModel model)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                    .ThenInclude(x => x.Packing)
                        .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(
                    x => x.Id == model.DispatchId);

            if (dispatch == null)
                return NotFound();

            // -----------------------------------------------------
            // Dispatch status validation
            // -----------------------------------------------------

            if (dispatch.Status != "In Transit")
            {
                ModelState.AddModelError(
                    "",
                    "Only In Transit dispatches can be delivered.");
            }

            // -----------------------------------------------------
            // Duplicate delivery validation
            // -----------------------------------------------------

            var existingDelivery =
                await _context.Deliveries
                    .FirstOrDefaultAsync(
                        x => x.DispatchId == model.DispatchId);

            if (existingDelivery != null)
            {
                ModelState.AddModelError(
                    "",
                    "A delivery record already exists for this dispatch.");
            }

            // -----------------------------------------------------
            // Quantity validation
            // -----------------------------------------------------

            if (model.DeliveryQuantity <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.DeliveryQuantity),
                    "Delivery quantity must be greater than zero.");
            }

            if (model.DeliveryQuantity > dispatch.Quantity)
            {
                ModelState.AddModelError(
                    nameof(model.DeliveryQuantity),
                    $"Delivery quantity cannot exceed dispatch quantity. " +
                    $"Dispatch quantity: {dispatch.Quantity} {dispatch.Unit}.");
            }

            if (model.DeliveryQuantity != dispatch.Quantity)
            {
                ModelState.AddModelError(
                    nameof(model.DeliveryQuantity),
                    $"Delivery quantity must equal dispatch quantity " +
                    $"({dispatch.Quantity} {dispatch.Unit}).");
            }

            // -----------------------------------------------------
            // Carton validation
            // -----------------------------------------------------

            if (model.TotalCartons <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    "Total cartons must be greater than zero.");
            }

            if (model.TotalCartons != dispatch.TotalCartons)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    $"Total cartons must equal dispatch cartons " +
                    $"({dispatch.TotalCartons}).");
            }

            // -----------------------------------------------------
            // Receiver validation
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(model.ReceiverName))
            {
                ModelState.AddModelError(
                    nameof(model.ReceiverName),
                    "Receiver name is required.");
            }

            // -----------------------------------------------------
            // Return validation errors
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                model.DispatchNumber =
                    dispatch.DispatchNumber;

                model.ShipmentNumber =
                    dispatch.Shipment?.ShipmentNumber ?? "N/A";

                model.ProductName =
                    dispatch.ProductName;

                model.Unit =
                    dispatch.Unit;

                model.DispatchQuantity =
                    dispatch.Quantity;

                model.DispatchCartons =
                    dispatch.TotalCartons;

                return View(model);
            }

            // -----------------------------------------------------
            // CREATE DELIVERY
            // -----------------------------------------------------

            var now = DateTime.UtcNow;

            var delivery = new Delivery
            {
                DeliveryNumber =
                    $"DL-{now:yyyyMMddHHmmssfff}",

                DispatchId =
                    dispatch.Id,

                ProductName =
                    dispatch.ProductName,

                Unit =
                    dispatch.Unit,

                Quantity =
                    model.DeliveryQuantity,

                TotalCartons =
                    model.TotalCartons,

                DeliveryDate =
                    model.DeliveryDate,

                ReceiverName =
                    model.ReceiverName,

                ReceiverPhone =
                    model.ReceiverPhone,

                DeliveryAddress =
                    model.DeliveryAddress,

                // IMPORTANT:
                // Finance creates it,
                // Admin must approve it.
                Status = "Pending",

                DeliveredBy =
                    CurrentUser(),

                CreatedAt =
                    now,

                Remarks =
                    model.Remarks
            };

            _context.Deliveries.Add(delivery);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Delivery {delivery.DeliveryNumber} created successfully " +
                "and submitted for Admin approval.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = delivery.Id
                });
        }

        // =========================================================
        // CONFIRM DELIVERED
        // =========================================================
        // Finance can execute final delivery ONLY after
        // Admin approval.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var delivery = await _context.Deliveries
                    .Include(x => x.Dispatch)
                        .ThenInclude(x => x.Shipment)
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

                if (delivery == null)
                    return NotFound();

                // -------------------------------------------------
                // ONLY ADMIN-APPROVED DELIVERY CAN BE COMPLETED
                // -------------------------------------------------

                if (delivery.Status != "Approved")
                {
                    TempData["ErrorMessage"] =
                        "Delivery cannot be confirmed until Admin approves it.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                // -------------------------------------------------
                // Dispatch must still be In Transit
                // -------------------------------------------------

                if (delivery.Dispatch.Status != "In Transit")
                {
                    TempData["ErrorMessage"] =
                        "The related dispatch is not In Transit.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                // -------------------------------------------------
                // FINAL DELIVERY
                // -------------------------------------------------

                delivery.Status =
                    "Delivered";

                delivery.DeliveredAt =
                    DateTime.UtcNow;

                delivery.DeliveredBy =
                    CurrentUser();

                delivery.Dispatch.Status =
                    "Delivered";

                delivery.Dispatch.Shipment.Status =
                    "Delivered";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Delivery {delivery.DeliveryNumber} completed successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Delivery completion failed: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }

        // =========================================================
        // CANCEL DELIVERY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var delivery =
                await _context.Deliveries
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (delivery == null)
                return NotFound();

            if (delivery.Status == "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Delivered records cannot be cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (delivery.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "This delivery is already cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            delivery.Status =
                "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Delivery {delivery.DeliveryNumber} cancelled successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}