using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateProductionProgressViewModel
    {
        public int ProductionMonitoringId { get; set; }

        public string ProductionReference { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal PlannedQuantity { get; set; }

        public decimal ProducedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Today's Produced Quantity")]
        public decimal TodayProducedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Today's Rejected Quantity")]
        public decimal TodayRejectedQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}