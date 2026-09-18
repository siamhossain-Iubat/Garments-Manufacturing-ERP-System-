using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreatePaymentViewModel
    {
        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string BuyerName { get; set; } = string.Empty;

        public decimal GrandTotal { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DueAmount { get; set; }

        [Required]
        [Range(
            typeof(decimal),
            "10.00",
            "500000.00",
            ErrorMessage = "Payment amount must be between 10 and 500000 BDT.")]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "BDT";

        public string PaymentMethod { get; set; } = "SSLCOMMERZ";

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}