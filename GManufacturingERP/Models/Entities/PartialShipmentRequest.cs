using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class PartialShipmentRequest
    {
        public int Id { get; set; }

        // Sales Order
        [Required]
        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        // Finished Goods Stock
        [Required]
        public int FinishedGoodsStockId { get; set; }

        public FinishedGoodsStock FinishedGoodsStock { get; set; } = null!;

        // Quantity requested for partial shipment
        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedQuantity { get; set; }

        // Quantity still remaining in the sales order
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingOrderQuantity { get; set; }

        // Pending / Approved / Declined / Completed
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        // When Finance requested buyer approval
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // When Buyer responded
        public DateTime? RespondedAt { get; set; }

        // Buyer ID
        [StringLength(100)]
        public string? BuyerId { get; set; }

        // Optional buyer remarks
        [StringLength(1000)]
        public string? BuyerRemarks { get; set; }
    }
}