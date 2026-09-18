using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.ViewModels
{
    public class CreateInvoiceViewModel
    {
        // =========================================================
        // DELIVERY INFORMATION
        // =========================================================

        public int DeliveryId { get; set; }

        public string DeliveryNumber { get; set; } = string.Empty;

        public string DispatchNumber { get; set; } = string.Empty;

        public string ShipmentNumber { get; set; } = string.Empty;


        // =========================================================
        // SALES ORDER INFORMATION
        // =========================================================

        public int? SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;


        // =========================================================
        // BUYER INFORMATION
        // =========================================================

        [Required]
        [StringLength(200)]
        public string BuyerName { get; set; } = string.Empty;


        // =========================================================
        // PRODUCT INFORMATION
        // =========================================================

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = "PCS";

        public decimal Quantity { get; set; }


        // =========================================================
        // COMMERCIAL VALUES
        // These are automatically loaded from Sales Order.
        // Sales Order contains values from accepted quotation.
        // =========================================================

        [Range(
            typeof(decimal),
            "0",
            "999999999")]
        public decimal UnitPrice { get; set; }


        [Range(
            typeof(decimal),
            "0",
            "999999999")]
        public decimal Discount { get; set; }


        [Range(
            typeof(decimal),
            "0",
            "999999999")]
        public decimal Tax { get; set; }


        public decimal Subtotal { get; set; }

        public decimal GrandTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvancePaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingDue { get; set; }


        // =========================================================
        // INVOICE DATE
        // =========================================================

        [Required]
        public DateTime InvoiceDate { get; set; }
            = DateTime.Today;


        // =========================================================
        // DUE DATE
        // =========================================================

        [Required]
        public DateTime DueDate { get; set; }
            = DateTime.Today.AddDays(30);





        // =========================================================
        // NOTES
        // =========================================================

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}