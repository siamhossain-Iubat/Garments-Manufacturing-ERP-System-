using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class AddMaterialRequirementViewModel
    {
        public int MaterialRequirementPlanId { get; set; }

        [Required]
        [Display(Name = "Material Name")]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Material Category")]
        public string MaterialCategory { get; set; } = string.Empty;

        [Required]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue)]
        [Display(Name = "Quantity Per Piece")]
        public decimal QuantityPerPiece { get; set; }

        [Range(0, 100)]
        [Display(Name = "Wastage Percentage")]
        public decimal WastagePercentage { get; set; }

        public string? Remarks { get; set; }
    }
}