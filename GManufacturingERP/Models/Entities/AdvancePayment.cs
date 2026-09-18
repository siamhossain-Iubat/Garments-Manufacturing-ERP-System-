using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class AdvancePayment
    {
        public int Id { get; set; }

        // Related quotation
        [Required]
        public int QuotationId { get; set; }

        public Quotation? Quotation { get; set; }

        // Buyer who made the payment
        [Required]
        public string BuyerId { get; set; } = string.Empty;

        // Payment amount
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01",
            "9999999999999999")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "BDT";

        // SSLCommerz information
        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }
            = Guid.NewGuid().ToString("N");

        [StringLength(100)]
        public string? ValidationId { get; set; }

        [StringLength(100)]
        public string? BankTransactionId { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }
            = "SSLCOMMERZ";

        // Pending, Processing, Paid, Failed, Cancelled
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}