using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateDeliveryViewModel
    {
        public int DispatchId { get; set; }

        public string DispatchNumber { get; set; } = string.Empty;

        public string ShipmentNumber { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        public decimal DispatchQuantity { get; set; }

        public int DispatchCartons { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal DeliveryQuantity { get; set; }

        [Required]
        [Range(1, 100000)]
        public int TotalCartons { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(150)]
        public string ReceiverName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReceiverPhone { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}