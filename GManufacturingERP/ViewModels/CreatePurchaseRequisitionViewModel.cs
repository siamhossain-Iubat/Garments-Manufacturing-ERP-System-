using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreatePurchaseRequisitionViewModel
    {
        public int MaterialRequirementPlanId { get; set; }

        public string MRPNumber { get; set; } = string.Empty;

        public string BuyerCompany { get; set; } = string.Empty;

        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }

        [Required]
        public string Priority { get; set; } = "Normal";

        public string? Remarks { get; set; }

        public List<PurchaseRequisitionItemViewModel> Items { get; set; }
            = new List<PurchaseRequisitionItemViewModel>();
    }

    public class PurchaseRequisitionItemViewModel
    {
        public string MaterialName { get; set; } = string.Empty;

        public string MaterialCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal RequestedQuantity { get; set; }

        public decimal EstimatedUnitPrice { get; set; }

        public decimal EstimatedTotalPrice =>
            RequestedQuantity * EstimatedUnitPrice;

        public string? Remarks { get; set; }
    }
}