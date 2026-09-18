using System;
using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateQuotationRequestViewModel
    {
        [Required]
        [StringLength(500)]
        [Display(Name = "Product Description")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "Pieces";

        [DataType(DataType.Date)]
        [Display(Name = "Required Delivery Date")]
        public DateTime? RequiredDeliveryDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Shipping Country")]
        public string? ShippingCountry { get; set; }

        [StringLength(500)]
        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}