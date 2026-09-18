using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class AdvancePaymentViewModel
    {
        public int QuotationId { get; set; }

        public string QuotationNumber { get; set; }
            = string.Empty;

        public decimal QuotationTotal { get; set; }

        public int AdvancePaymentPercentage { get; set; }

        public decimal AdvancePaymentAmount { get; set; }

        public string BuyerName { get; set; }
            = string.Empty;

        [Required]
        [EmailAddress]
        public string BuyerEmail { get; set; }
            = string.Empty;

        [Required]
        public string BuyerPhone { get; set; }
            = string.Empty;

        public string? ShippingAddress { get; set; }
    }
}