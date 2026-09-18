using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreatePackingViewModel
    {
        // Finished Goods Stock
        [Required]
        public int FinishedGoodsStockId { get; set; }

        // Sales Order
        public int? SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        // Product
        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        // Quantity Information
        public decimal OrderedQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

        public decimal AlreadyPackedQuantity { get; set; }

        public decimal RemainingOrderQuantity { get; set; }

        public decimal QuantityAvailableForPacking { get; set; }

        // Packing Information
        [Required]
        [DataType(DataType.Date)]
        public DateTime PackingDate { get; set; } = DateTime.Today;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PackingQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalCartons { get; set; }

        [Range(0, double.MaxValue)]
        public decimal QuantityPerCarton { get; set; }

        // Shipment
        public string ShipmentType { get; set; } = "FullShipment";

        public bool PartialShipmentApproved { get; set; }

        public string? ApprovedBy { get; set; }
        public int? PartialShipmentRequestId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Remarks
        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}