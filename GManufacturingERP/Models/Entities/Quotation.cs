using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Quotation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string QuotationNumber { get; set; } = string.Empty;

        // =====================================================
        // BUYER INFORMATION
        // =====================================================

        [Required]
        public string BuyerId { get; set; } = string.Empty;

        public ApplicationUser? Buyer { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(150)]
        public string? BuyerEmail { get; set; }

        [StringLength(50)]
        public string? BuyerPhone { get; set; }

        // =====================================================
        // PRODUCT / REQUEST INFORMATION
        // =====================================================

        [Required]
        [StringLength(500)]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "Pieces";

        // =====================================================
        // PRICE INFORMATION
        // =====================================================

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

        // =====================================================
        // DELIVERY INFORMATION
        // =====================================================

        public DateTime? RequiredDeliveryDate { get; set; }

        [StringLength(100)]
        public string? ShippingCountry { get; set; }

        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        // =====================================================
        // QUOTATION DATES
        // =====================================================

        [Required]
        public DateTime QuotationDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime ValidUntil { get; set; } =
            DateTime.Today.AddDays(30);

        // =====================================================
        // WORKFLOW STATUS
        // =====================================================

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Requested";

        // Requested
        // Under Review
        // Prepared
        // Sent
        // Accepted
        // Rejected
        // Cancelled
        // Converted

        // =====================================================
        // TERMS / REMARKS
        // =====================================================

        [StringLength(1000)]
        public string? TermsAndConditions { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        // =====================================================
        // TRACKING
        // =====================================================

        [StringLength(100)]
        public string? RequestedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public DateTime? PreparedAt { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        // =====================================================
        // SALES ORDER CONNECTION
        // =====================================================

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        // =====================================================
        // ADVANCE PAYMENT INFORMATION
        // =====================================================

        [Required]
        [Range(0, 100)]
        public int AdvancePaymentPercentage { get; set; } = 20;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvancePaymentAmount { get; set; }
    }
}