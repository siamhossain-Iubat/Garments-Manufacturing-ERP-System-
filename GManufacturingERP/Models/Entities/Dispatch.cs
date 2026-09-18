using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class Dispatch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string DispatchNumber { get; set; } = string.Empty;

        [Required]
        public int ShipmentId { get; set; }

        public Shipment Shipment { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        public decimal Quantity { get; set; }

        public int TotalCartons { get; set; }

        [Required]
        public DateTime DispatchDate { get; set; } = DateTime.Today;

        [StringLength(150)]
        public string? TransportName { get; set; }

        [StringLength(100)]
        public string? VehicleNumber { get; set; }

        [StringLength(150)]
        public string? DriverName { get; set; }

        [StringLength(50)]
        public string? DriverPhone { get; set; }

        [StringLength(500)]
        public string? Destination { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DispatchedAt { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}