using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class PrepareQuotationViewModel
    {
        public int Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string? BuyerEmail { get; set; }

        public string? BuyerPhone { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Unit { get; set; } = "Pieces";

        // =====================================================
        // SALES MANAGER INPUT
        // =====================================================

        [Required]
        [Range(typeof(decimal), "0.01", "999999999")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Range(typeof(decimal), "0", "999999999")]
        public decimal Discount { get; set; }

        [Range(typeof(decimal), "0", "999999999")]
        public decimal Tax { get; set; }

        public decimal Subtotal { get; set; }

        public decimal GrandTotal { get; set; }

        // =====================================================
        // DELIVERY
        // =====================================================

        [DataType(DataType.Date)]
        public DateTime? RequiredDeliveryDate { get; set; }

        public string? ShippingCountry { get; set; }

        public string? ShippingAddress { get; set; }

        // =====================================================
        // QUOTATION VALIDITY
        // =====================================================

        [Required]
        [DataType(DataType.Date)]
        public DateTime ValidUntil { get; set; } =
            DateTime.Today.AddDays(30);

        // =====================================================
        // TERMS
        // =====================================================

        [StringLength(1000)]
        public string? TermsAndConditions { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        // =====================================================
        // ADVANCE PAYMENT TERMS
        // =====================================================

        [Required]
        [Range(0, 100)]
        [Display(Name = "Advance Payment Percentage")]
        public int AdvancePaymentPercentage { get; set; } = 20;

        public decimal AdvancePaymentAmount { get; set; }
    }
}