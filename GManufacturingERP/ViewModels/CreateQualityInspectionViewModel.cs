using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateQualityInspectionViewModel
    {
        [Required]
        public int ProductionMonitoringId { get; set; }

        // Automatic Information
        public string ProductionReference { get; set; }
            = string.Empty;

        public string ProductName { get; set; }
            = string.Empty;

        public decimal ProducedQuantity { get; set; }

        // Manual Input
        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Inspected Quantity")]
        public decimal InspectedQuantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Passed Quantity")]
        public decimal PassedQuantity { get; set; }

        // Automatic Calculation
        [Display(Name = "Failed Quantity")]
        public decimal FailedQuantity { get; set; }

        // Manual Input
        [Display(Name = "Defect Description")]
        public string? DefectDescription { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }
    }
}