namespace GManufacturingERP.Models.Entities
{
    public class MaterialRequirementItem
    {
        public int Id { get; set; }

        public int MaterialRequirementPlanId { get; set; }

        public MaterialRequirementPlan?
            MaterialRequirementPlan
        { get; set; }

        public string MaterialName { get; set; } = string.Empty;

        public string MaterialCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal QuantityPerPiece { get; set; }

        public decimal WastagePercentage { get; set; }

        public decimal RequiredQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}