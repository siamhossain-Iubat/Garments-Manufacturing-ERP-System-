using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class ReworkFinishedGoodsViewModel
    {
        public int FinishedGoodsStockId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        public decimal OrderedQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

        public decimal ReworkQuantity { get; set; }

        public string ProductionReference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter rework quantity.")]
        [Range(0.01, double.MaxValue,
            ErrorMessage = "Rework quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}