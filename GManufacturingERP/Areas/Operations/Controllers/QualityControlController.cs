using System.Security.Claims;
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
    public class QualityControlController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QualityControlController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var inspections =
                await _context.QualityInspections
                    .Include(x => x.ProductionMonitoring)
                    .OrderByDescending(x => x.InspectionDate)
                    .ToListAsync();

            return View(inspections);
        }


        // =====================================================
        // CREATE QC - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int productionMonitoringId)
        {
            var production =
                await _context.ProductionMonitorings
                    .FirstOrDefaultAsync(x =>
                        x.Id == productionMonitoringId);

            if (production == null)
            {
                return NotFound();
            }


            // -------------------------------------------------
            // QC can only be created for completed production
            // -------------------------------------------------

            if (production.Status != "Completed")
            {
                TempData["Error"] =
                    "Quality inspection can only be created for completed production.";

                return RedirectToAction(
                    "Details",
                    "ProductionMonitoring",
                    new
                    {
                        area = "Operations",
                        id = productionMonitoringId
                    });
            }


            // -------------------------------------------------
            // Prevent duplicate approved QC
            // -------------------------------------------------

            var existingInspection =
                await _context.QualityInspections
                    .AnyAsync(x =>
                        x.ProductionMonitoringId ==
                        productionMonitoringId
                        &&
                        x.Status == "Approved");

            if (existingInspection)
            {
                TempData["Error"] =
                    "Approved quality inspection already exists.";

                return RedirectToAction(nameof(Index));
            }


            // -------------------------------------------------
            // Create ViewModel
            // -------------------------------------------------

            var model =
                new CreateQualityInspectionViewModel
                {
                    ProductionMonitoringId =
                        production.Id,

                    ProductionReference =
                        production.ProductionReference,

                    ProductName =
                        production.ProductName,

                    ProducedQuantity =
                        production.ProducedQuantity,

                    InspectedQuantity =
                        production.ProducedQuantity,

                    PassedQuantity = 0,

                    FailedQuantity =
                        production.ProducedQuantity
                };

            return View(model);
        }


        // =====================================================
        // CREATE QC - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateQualityInspectionViewModel model)
        {
            var production =
                await _context.ProductionMonitorings
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        model.ProductionMonitoringId);

            if (production == null)
            {
                return NotFound();
            }


            // -------------------------------------------------
            // Load trusted production information
            // -------------------------------------------------

            model.ProductionReference =
                production.ProductionReference;

            model.ProductName =
                production.ProductName;

            model.ProducedQuantity =
                production.ProducedQuantity;


            // -------------------------------------------------
            // Production must be completed
            // -------------------------------------------------

            if (production.Status != "Completed")
            {
                ModelState.AddModelError(
                    "",
                    "Only completed production can be inspected.");
            }


            // -------------------------------------------------
            // Inspected Quantity Validation
            // -------------------------------------------------

            if (model.InspectedQuantity <= 0)
            {
                ModelState.AddModelError(
                    "InspectedQuantity",
                    "Inspected quantity must be greater than zero.");
            }


            if (model.InspectedQuantity >
                production.ProducedQuantity)
            {
                ModelState.AddModelError(
                    "InspectedQuantity",
                    "Inspected quantity cannot exceed produced quantity.");
            }


            // -------------------------------------------------
            // Passed Quantity Validation
            // -------------------------------------------------

            if (model.PassedQuantity < 0)
            {
                ModelState.AddModelError(
                    "PassedQuantity",
                    "Passed quantity cannot be negative.");
            }


            if (model.PassedQuantity >
                model.InspectedQuantity)
            {
                ModelState.AddModelError(
                    "PassedQuantity",
                    "Passed quantity cannot exceed inspected quantity.");
            }


            // -------------------------------------------------
            // Failed Quantity
            //
            // Failed = Inspected - Passed
            // -------------------------------------------------

            model.FailedQuantity =
                model.InspectedQuantity -
                model.PassedQuantity;


            if (model.FailedQuantity < 0)
            {
                model.FailedQuantity = 0;
            }


            // -------------------------------------------------
            // Validate duplicate approved inspection
            // -------------------------------------------------

            var approvedInspectionExists =
                await _context.QualityInspections
                    .AnyAsync(x =>
                        x.ProductionMonitoringId ==
                        production.Id
                        &&
                        x.Status == "Approved");

            if (approvedInspectionExists)
            {
                ModelState.AddModelError(
                    "",
                    "An approved quality inspection already exists for this production.");
            }


            // -------------------------------------------------
            // Return View if invalid
            // -------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // -------------------------------------------------
            // Determine inspection result
            // -------------------------------------------------

            string inspectionResult;

            if (model.PassedQuantity ==
                model.InspectedQuantity)
            {
                inspectionResult = "Passed";
            }
            else if (model.PassedQuantity == 0)
            {
                inspectionResult = "Failed";
            }
            else
            {
                inspectionResult = "Partially Passed";
            }


            // -------------------------------------------------
            // Create QC Inspection
            // -------------------------------------------------

            var inspection =
                new QualityInspection
                {
                    InspectionNumber =
                        GenerateInspectionNumber(),

                    ProductionMonitoringId =
                        production.Id,

                    ProductionReference =
                        production.ProductionReference,

                    ProductName =
                        production.ProductName,

                    ProducedQuantity =
                        production.ProducedQuantity,

                    InspectedQuantity =
                        model.InspectedQuantity,

                    PassedQuantity =
                        model.PassedQuantity,

                    FailedQuantity =
                        model.FailedQuantity,

                    InspectionResult =
                        inspectionResult,

                    // Important:
                    // Operations creates the QC.
                    // Admin will approve it later.
                    Status = "Draft",

                    InspectionDate =
                        DateTime.UtcNow,

                    InspectedBy =
                        User.FindFirstValue(
                            ClaimTypes.Name)
                        ?? User.Identity?.Name
                        ?? "System",

                    DefectDescription =
                        model.DefectDescription,

                    Remarks =
                        model.Remarks
                };


            _context.QualityInspections.Add(
                inspection);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Quality inspection {inspection.InspectionNumber} created successfully.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = inspection.Id
                });
        }


        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var inspection =
                await _context.QualityInspections
                    .Include(x =>
                        x.ProductionMonitoring)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }

            return View(inspection);
        }


        // =====================================================
        // SUBMIT QC
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int id)
        {
            var inspection =
                await _context.QualityInspections
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }


            if (inspection.Status != "Draft")
            {
                TempData["Error"] =
                    "Only draft inspections can be submitted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            inspection.Status =
                "Submitted";


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Quality inspection {inspection.InspectionNumber} submitted for Admin approval.";


            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =====================================================
        // INSPECTION NUMBER
        // =====================================================

        private static string GenerateInspectionNumber()
        {
            return $"QC-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}