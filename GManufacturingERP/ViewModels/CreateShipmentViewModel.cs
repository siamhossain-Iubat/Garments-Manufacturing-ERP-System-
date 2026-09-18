using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateShipmentViewModel
    {
        public int PackingId { get; set; }

        public string PackingNumber { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        public decimal PackedQuantity { get; set; }

        public decimal AlreadyShippedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal ShipmentQuantity { get; set; }

        [Required]
        [Range(1, 100000)]
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

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}