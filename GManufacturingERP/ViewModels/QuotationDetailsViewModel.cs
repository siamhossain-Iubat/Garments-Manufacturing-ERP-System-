using System;

namespace GManufacturingERP.ViewModels
{
    public class QuotationDetailsViewModel
    {
        public int Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public string BuyerId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string? BuyerEmail { get; set; }

        public string? BuyerPhone { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Unit { get; set; } = "Pieces";

        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal GrandTotal { get; set; }

        public DateTime? RequiredDeliveryDate { get; set; }

        public string? ShippingCountry { get; set; }

        public string? ShippingAddress { get; set; }

        public DateTime QuotationDate { get; set; }

        public DateTime ValidUntil { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? TermsAndConditions { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        public int? SalesOrderId { get; set; }
    }
}