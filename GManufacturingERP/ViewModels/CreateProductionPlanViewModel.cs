using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateProductionPlanViewModel
    {
        public int SalesOrderId { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string ProductDescription { get; set; } = string.Empty;

        public string BuyerCompany { get; set; } = string.Empty;

        public int OrderedQuantity { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Planned Quantity")]
        public int PlannedQuantity { get; set; }

        [Required]
        [Display(Name = "Planned Start Date")]
        [DataType(DataType.Date)]
        public DateTime PlannedStartDate { get; set; }

        [Required]
        [Display(Name = "Expected Completion Date")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCompletionDate { get; set; }

        [Display(Name = "Production Remarks")]
        public string? ProductionRemarks { get; set; }
    }
}