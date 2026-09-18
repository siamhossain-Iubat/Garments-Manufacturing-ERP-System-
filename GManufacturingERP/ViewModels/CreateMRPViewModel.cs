using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateMRPViewModel
    {
        public int ProductionPlanId { get; set; }

        public string PlanNumber { get; set; } = string.Empty;

        public string ProductDescription { get; set; } = string.Empty;

        public string BuyerCompany { get; set; } = string.Empty;

        public int PlannedQuantity { get; set; }

        [Display(Name = "MRP Remarks")]
        public string? Remarks { get; set; }
    }
}