using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class FinishedGoodsStock
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProductCode { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        public int? ProductionPlanId { get; set; }

        public ProductionPlan? ProductionPlan { get; set; }

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        [StringLength(100)]
        public string? ProductionReference { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OrderedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AvailableQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReservedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReworkQuantity { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FinishedGoodsTransaction> Transactions { get; set; }
            = new List<FinishedGoodsTransaction>();
    }
}