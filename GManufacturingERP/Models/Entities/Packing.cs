using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Packing
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PackingNumber { get; set; } = string.Empty;


        // ==========================================
        // Sales Order
        // ==========================================

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }


        // ==========================================
        // Packing Date
        // ==========================================

        [Required]
        public DateTime PackingDate { get; set; }
            = DateTime.UtcNow;


        // ==========================================
        // Quantity
        // ==========================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalQuantity { get; set; }

        public int TotalCartons { get; set; }


        // ==========================================
        // Status
        // ==========================================

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";


        // ==========================================
        // Shipment Type
        // ==========================================

        [Required]
        [StringLength(30)]
        public string ShipmentType { get; set; }
            = "FullShipment";


        // ==========================================
        // Partial Shipment Approval
        // ==========================================

        public bool PartialShipmentApproved { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }


        // ==========================================
        // Packed By
        // ==========================================

        [StringLength(100)]
        public string? PackedBy { get; set; }

        public DateTime? PackedAt { get; set; }

        public int? PartialShipmentRequestId { get; set; }

        public PartialShipmentRequest? PartialShipmentRequest { get; set; }


        // ==========================================
        // Remarks
        // ==========================================

        [StringLength(1000)]
        public string? Remarks { get; set; }


        // ==========================================
        // Audit
        // ==========================================

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;


        // ==========================================
        // Items
        // ==========================================

        public ICollection<PackingItem> Items { get; set; }
            = new List<PackingItem>();
        public ICollection<Shipment> Shipments { get; set; }
    = new List<Shipment>();
    }
}