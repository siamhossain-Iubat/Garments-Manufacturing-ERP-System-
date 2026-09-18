using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreatePurchaseOrderViewModel
    {
        public int PurchaseRequisitionId { get; set; }

        public string RequisitionNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Supplier Contact")]
        public string? SupplierContact { get; set; }

        [Display(Name = "Supplier Address")]
        public string? SupplierAddress { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpectedDeliveryDate { get; set; }

        public string? PaymentTerms { get; set; }

        public string? DeliveryTerms { get; set; }

        public string? Remarks { get; set; }

        public List<CreatePurchaseOrderItemViewModel> Items { get; set; }
            = new();
    }

    public class CreatePurchaseOrderItemViewModel
    {
        public string MaterialName { get; set; } = string.Empty;

        public string? MaterialCategory { get; set; }

        public string? Unit { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal OrderedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string? Remarks { get; set; }
    }
}