using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class SalesOrder
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string BuyerId { get; set; } = string.Empty;

        public ApplicationUser? Buyer { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string? BuyerEmail { get; set; }

        public string? BuyerPhone { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string? Unit { get; set; } = "Pieces";

        public DateTime? RequiredDeliveryDate { get; set; }

        public string? ShippingCountry { get; set; }

        public string? ShippingAddress { get; set; }

        public string? SpecialInstructions { get; set; }


        // =========================================================
        // COMMERCIAL VALUES
        // These values are copied from the accepted quotation.
        // =========================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }


        // =========================================================
        // ORDER INFORMATION
        // =========================================================

        public string OrderStatus { get; set; } = "Pending";

        public string? SalesRemarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}