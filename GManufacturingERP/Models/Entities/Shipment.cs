using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Shipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ShipmentNumber { get; set; } = string.Empty;

        [Required]
        public int PackingId { get; set; }

        public Packing Packing { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        public int TotalCartons { get; set; }

        [Required]
        public DateTime ShipmentDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [StringLength(150)]
        public string? TransportName { get; set; }

        [StringLength(100)]
        public string? VehicleNumber { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DispatchedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}