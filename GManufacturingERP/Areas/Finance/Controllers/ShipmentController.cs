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
    [Authorize(Roles = "FinanceLogisticsManager,Admin")]
    public class ShipmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShipmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // GET: Finance/Shipment
        public async Task<IActionResult> Index()
        {
            var shipments = await _context.Shipments
                .Include(x => x.Packing)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(shipments);
        }

        // GET: Finance/Shipment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var shipment = await _context.Shipments
                .Include(x => x.Packing)
                    .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            return View(shipment);
        }

        // GET: Finance/Shipment/Create?packingId=5
        [HttpGet]
        public async Task<IActionResult> Create(int packingId)
        {
            var packing = await _context.Packings
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == packingId);

            if (packing == null)
                return NotFound();

            if (packing.Status != "Packed")
            {
                TempData["ErrorMessage"] =
                    "Only Packed records can be used to create a shipment.";

                return RedirectToAction(
                    "Details",
                    "Packing",
                    new
                    {
                        area = "Finance",
                        id = packingId
                    });
            }

            var alreadyShipped = await _context.Shipments
                .Where(x =>
                    x.PackingId == packingId &&
                    x.Status != "Cancelled")
                .SumAsync(x => (decimal?)x.Quantity) ?? 0m;

            var remainingQuantity =
                packing.TotalQuantity - alreadyShipped;

            if (remainingQuantity <= 0)
            {
                TempData["ErrorMessage"] =
                    "This packing has no remaining quantity available for shipment.";

                return RedirectToAction(
                    "Details",
                    "Packing",
                    new
                    {
                        area = "Finance",
                        id = packingId
                    });
            }

            var model = new CreateShipmentViewModel
            {
                PackingId = packing.Id,
                PackingNumber = packing.PackingNumber,
                ProductName = packing.Items
                    .Select(x => x.ProductName)
                    .FirstOrDefault() ?? "N/A",
                Unit = packing.Items
                    .Select(x => x.Unit)
                    .FirstOrDefault() ?? "PCS",

                PackedQuantity = packing.TotalQuantity,
                AlreadyShippedQuantity = alreadyShipped,
                RemainingQuantity = remainingQuantity,

                ShipmentQuantity = remainingQuantity,
                TotalCartons = packing.TotalCartons,
                ShipmentDate = DateTime.Today,

                ShippingAddress = packing.SalesOrder?.ShippingAddress
            };

            return View(model);
        }

        // POST: Finance/Shipment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateShipmentViewModel model)
        {
            var packing = await _context.Packings
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == model.PackingId);

            if (packing == null)
                return NotFound();

            if (packing.Status != "Packed")
            {
                ModelState.AddModelError(
                    "",
                    "Only Packed records can be shipped.");
            }

            var alreadyShipped = await _context.Shipments
                .Where(x =>
                    x.PackingId == model.PackingId &&
                    x.Status != "Cancelled")
                .SumAsync(x => (decimal?)x.Quantity) ?? 0m;

            var remainingQuantity =
                packing.TotalQuantity - alreadyShipped;

            if (model.ShipmentQuantity <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.ShipmentQuantity),
                    "Shipment quantity must be greater than zero.");
            }

            if (model.ShipmentQuantity > remainingQuantity)
            {
                ModelState.AddModelError(
                    nameof(model.ShipmentQuantity),
                    $"Shipment quantity cannot exceed remaining quantity. " +
                    $"Remaining: {remainingQuantity} {packing.Items.FirstOrDefault()?.Unit ?? "PCS"}.");
            }

            if (model.TotalCartons <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    "Total cartons must be greater than zero.");
            }

            if (model.TotalCartons > packing.TotalCartons)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    $"Total cartons cannot exceed packed cartons ({packing.TotalCartons}).");
            }

            if (!ModelState.IsValid)
            {
                model.PackingNumber = packing.PackingNumber;

                model.ProductName = packing.Items
                    .Select(x => x.ProductName)
                    .FirstOrDefault() ?? "N/A";

                model.Unit = packing.Items
                    .Select(x => x.Unit)
                    .FirstOrDefault() ?? "PCS";

                model.PackedQuantity = packing.TotalQuantity;
                model.AlreadyShippedQuantity = alreadyShipped;
                model.RemainingQuantity = remainingQuantity;

                return View(model);
            }

            var now = DateTime.UtcNow;

            var shipment = new Shipment
            {
                ShipmentNumber = $"SH-{now:yyyyMMddHHmmssfff}",

                PackingId = packing.Id,

                ProductName = packing.Items
                    .Select(x => x.ProductName)
                    .FirstOrDefault() ?? "N/A",

                Unit = packing.Items
                    .Select(x => x.Unit)
                    .FirstOrDefault() ?? "PCS",

                Quantity = model.ShipmentQuantity,

                TotalCartons = model.TotalCartons,

                ShipmentDate = model.ShipmentDate,

                ShippingAddress = model.ShippingAddress,

                TransportName = model.TransportName,

                VehicleNumber = model.VehicleNumber,

                TrackingNumber = model.TrackingNumber,

                Status = "Draft",

                CreatedBy = CurrentUser(),

                CreatedAt = now,

                Remarks = model.Remarks
            };

            _context.Shipments.Add(shipment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} created successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = shipment.Id
                });
        }

        // POST: Finance/Shipment/ReadyForDispatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReadyForDispatch(int id)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft shipments can be marked as Ready for Dispatch.";

                return RedirectToAction(nameof(Details), new { id });
            }

            shipment.Status = "Ready for Dispatch";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} is ready for dispatch.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Shipment/Dispatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(int id)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "Ready for Dispatch")
            {
                TempData["ErrorMessage"] =
                    "Only shipments marked Ready for Dispatch can be dispatched.";

                return RedirectToAction(nameof(Details), new { id });
            }

            shipment.Status = "Dispatched";
            shipment.DispatchedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} dispatched successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Shipment/InTransit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InTransit(int id)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "Dispatched")
            {
                TempData["ErrorMessage"] =
                    "Only Dispatched shipments can be marked In Transit.";

                return RedirectToAction(nameof(Details), new { id });
            }

            shipment.Status = "In Transit";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} is now In Transit.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Shipment/Deliver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deliver(int id)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "In Transit")
            {
                TempData["ErrorMessage"] =
                    "Only In Transit shipments can be marked as Delivered.";

                return RedirectToAction(nameof(Details), new { id });
            }

            shipment.Status = "Delivered";
            shipment.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} delivered successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Shipment/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.Status == "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Delivered shipments cannot be cancelled.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (shipment.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "This shipment is already cancelled.";

                return RedirectToAction(nameof(Details), new { id });
            }

            shipment.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Shipment {shipment.ShipmentNumber} cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}