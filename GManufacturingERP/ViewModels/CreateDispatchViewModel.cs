using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateDispatchViewModel
    {
        public int ShipmentId { get; set; }

        public string ShipmentNumber { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        public decimal ShipmentQuantity { get; set; }

        public int ShipmentCartons { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal DispatchQuantity { get; set; }

        [Required]
        [Range(1, 100000)]
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

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}