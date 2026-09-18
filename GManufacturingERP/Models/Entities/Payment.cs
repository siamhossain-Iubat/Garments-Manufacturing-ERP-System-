using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentNumber { get; set; } = string.Empty;

        [Required]
        public int InvoiceId { get; set; }

        public Invoice Invoice { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string BuyerName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Currency { get; set; } = "BDT";

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "SSLCOMMERZ";

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ValidationId { get; set; }

        [StringLength(100)]
        public string? BankTransactionId { get; set; }

        [StringLength(100)]
        public string? CardType { get; set; }

        [StringLength(100)]
        public string? CardBrand { get; set; }

        [StringLength(100)]
        public string? CardIssuer { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}