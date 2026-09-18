using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateFinishedGoodsViewModel
    {
        public int QualityInspectionId { get; set; }

        public string InspectionNumber { get; set; } = string.Empty;

        public string ProductionReference { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal PassedQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}