using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateSalesOrderViewModel
    {
        [Required]
        [Display(Name = "Product Description")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Required Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime? RequiredDeliveryDate { get; set; }

        [Display(Name = "Shipping Country")]
        public string? ShippingCountry { get; set; }

        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [Display(Name = "Special Instructions")]
        public string? SpecialInstructions { get; set; }
    }
}