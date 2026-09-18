using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class FinishedGoodsRework
    {
        public int Id { get; set; }

        [Required]
        public int FinishedGoodsStockId { get; set; }

        public FinishedGoodsStock? FinishedGoodsStock { get; set; }

        [Required]
        public int ProductionMonitoringId { get; set; }

        public ProductionMonitoring? ProductionMonitoring { get; set; }

        [Required]
        [StringLength(50)]
        public string ReworkNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PassedQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RejectedQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "In Production";

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}