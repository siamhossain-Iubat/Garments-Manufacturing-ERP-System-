using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GManufacturingERP.ViewModels
{
    public class CreateMaterialIssueViewModel
    {
        [Required(ErrorMessage = "Please select an approved MRP.")]
        [Display(Name = "Approved MRP")]
        public int? MaterialRequirementPlanId { get; set; }

        public List<SelectListItem> ApprovedMRPs { get; set; }
            = new List<SelectListItem>();

        [Display(Name = "Production Reference")]
        public string ProductionReference { get; set; }
            = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; }
            = string.Empty;

        [Display(Name = "Planned Quantity")]
        public decimal PlannedQuantity { get; set; }

        [Display(Name = "Unit")]
        public string Unit { get; set; }
            = string.Empty;

        public string? Remarks { get; set; }

        public List<CreateMaterialIssueItemViewModel> Items { get; set; }
            = new List<CreateMaterialIssueItemViewModel>();
    }

    public class CreateMaterialIssueItemViewModel
    {
        public int InventoryStockId { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string MaterialCategory { get; set; }
            = string.Empty;

        public string Unit { get; set; }
            = string.Empty;

        public decimal RequiredQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal IssuedQuantity { get; set; }
    }
}