using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GManufacturingERP.ViewModels
{
    public class CreateProductionMonitoringViewModel
    {
        [Required]
        [Display(Name = "Production Reference")]
        public string ProductionReference { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Planned Quantity")]
        public decimal PlannedQuantity { get; set; }

        [Required]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public string? Remarks { get; set; }

        // Selected Approved Material Issue
        [Required]
        [Display(Name = "Approved Material Issue")]
        public int MaterialIssueId { get; set; }

        // Approved Material Issue dropdown
        public List<SelectListItem> ApprovedMaterialIssues { get; set; }
            = new List<SelectListItem>();
    }
}