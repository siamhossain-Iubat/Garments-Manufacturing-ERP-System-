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
    public class DispatchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DispatchController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "System";
        }

        // GET: Finance/Dispatch
        public async Task<IActionResult> Index()
        {
            var dispatches = await _context.Dispatches
                .Include(x => x.Shipment)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(dispatches);
        }

        // GET: Finance/Dispatch/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                    .ThenInclude(x => x.Packing)
                        .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dispatch == null)
                return NotFound();

            return View(dispatch);
        }

        // GET: Finance/Dispatch/Create?shipmentId=5
        [HttpGet]
        public async Task<IActionResult> Create(int shipmentId)
        {
            var shipment = await _context.Shipments
                .Include(x => x.Packing)
                    .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == shipmentId);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "Ready for Dispatch")
            {
                TempData["ErrorMessage"] =
                    "Only shipments marked Ready for Dispatch can be dispatched.";

                return RedirectToAction(
                    "Details",
                    "Shipment",
                    new
                    {
                        area = "Finance",
                        id = shipmentId
                    });
            }

            var existingDispatch = await _context.Dispatches
                .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId);

            if (existingDispatch != null)
            {
                TempData["ErrorMessage"] =
                    "A dispatch record already exists for this shipment.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = existingDispatch.Id
                    });
            }

            var model = new CreateDispatchViewModel
            {
                ShipmentId = shipment.Id,

                ShipmentNumber = shipment.ShipmentNumber,

                ProductName = shipment.ProductName,

                Unit = shipment.Unit,

                ShipmentQuantity = shipment.Quantity,

                ShipmentCartons = shipment.TotalCartons,

                DispatchQuantity = shipment.Quantity,

                TotalCartons = shipment.TotalCartons,

                DispatchDate = DateTime.Today,

                TransportName = shipment.TransportName,

                VehicleNumber = shipment.VehicleNumber,

                Destination = shipment.ShippingAddress
            };

            return View(model);
        }

        // POST: Finance/Dispatch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDispatchViewModel model)
        {
            var shipment = await _context.Shipments
                .Include(x => x.Packing)
                    .ThenInclude(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == model.ShipmentId);

            if (shipment == null)
                return NotFound();

            if (shipment.Status != "Ready for Dispatch")
            {
                ModelState.AddModelError(
                    "",
                    "Only shipments marked Ready for Dispatch can be dispatched.");
            }

            var existingDispatch = await _context.Dispatches
                .FirstOrDefaultAsync(x => x.ShipmentId == model.ShipmentId);

            if (existingDispatch != null)
            {
                ModelState.AddModelError(
                    "",
                    "A dispatch record already exists for this shipment.");
            }

            if (model.DispatchQuantity <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.DispatchQuantity),
                    "Dispatch quantity must be greater than zero.");
            }

            if (model.DispatchQuantity > shipment.Quantity)
            {
                ModelState.AddModelError(
                    nameof(model.DispatchQuantity),
                    $"Dispatch quantity cannot exceed shipment quantity. " +
                    $"Shipment quantity: {shipment.Quantity} {shipment.Unit}.");
            }

            if (model.TotalCartons <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    "Total cartons must be greater than zero.");
            }

            if (model.TotalCartons > shipment.TotalCartons)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCartons),
                    $"Cartons cannot exceed shipment cartons ({shipment.TotalCartons}).");
            }

            if (!ModelState.IsValid)
            {
                model.ShipmentNumber = shipment.ShipmentNumber;
                model.ProductName = shipment.ProductName;
                model.Unit = shipment.Unit;
                model.ShipmentQuantity = shipment.Quantity;
                model.ShipmentCartons = shipment.TotalCartons;

                return View(model);
            }

            var now = DateTime.UtcNow;

            var dispatch = new Dispatch
            {
                DispatchNumber = $"DP-{now:yyyyMMddHHmmssfff}",

                ShipmentId = shipment.Id,

                ProductName = shipment.ProductName,

                Unit = shipment.Unit,

                Quantity = model.DispatchQuantity,

                TotalCartons = model.TotalCartons,

                DispatchDate = model.DispatchDate,

                TransportName = model.TransportName,

                VehicleNumber = model.VehicleNumber,

                DriverName = model.DriverName,

                DriverPhone = model.DriverPhone,

                Destination = model.Destination,

                Status = "Draft",

                CreatedBy = CurrentUser(),

                CreatedAt = now,

                Remarks = model.Remarks
            };

            _context.Dispatches.Add(dispatch);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Dispatch {dispatch.DispatchNumber} created successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = dispatch.Id
                });
        }

        // POST: Finance/Dispatch/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dispatch == null)
                return NotFound();

            if (dispatch.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft dispatch records can be confirmed.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (dispatch.Shipment.Status != "Ready for Dispatch")
            {
                TempData["ErrorMessage"] =
                    "The related shipment is not Ready for Dispatch.";

                return RedirectToAction(nameof(Details), new { id });
            }

            dispatch.Status = "Dispatched";
            dispatch.DispatchedAt = DateTime.UtcNow;

            dispatch.Shipment.Status = "Dispatched";
            dispatch.Shipment.DispatchedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Dispatch {dispatch.DispatchNumber} confirmed successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Dispatch/InTransit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InTransit(int id)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dispatch == null)
                return NotFound();

            if (dispatch.Status != "Dispatched")
            {
                TempData["ErrorMessage"] =
                    "Only Dispatched records can be marked In Transit.";

                return RedirectToAction(nameof(Details), new { id });
            }

            dispatch.Status = "In Transit";

            dispatch.Shipment.Status = "In Transit";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Dispatch {dispatch.DispatchNumber} is now In Transit.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Finance/Dispatch/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var dispatch = await _context.Dispatches
                .Include(x => x.Shipment)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dispatch == null)
                return NotFound();

            if (dispatch.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only Draft dispatch records can be cancelled.";

                return RedirectToAction(nameof(Details), new { id });
            }

            dispatch.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Dispatch {dispatch.DispatchNumber} cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}