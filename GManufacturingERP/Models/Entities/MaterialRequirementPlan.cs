namespace GManufacturingERP.Models.Entities
{
    public class MaterialRequirementPlan
    {
        public int Id { get; set; }

        public string MRPNumber { get; set; } = string.Empty;

        public int ProductionPlanId { get; set; }

        public ProductionPlan? ProductionPlan { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public string BuyerCompany { get; set; } = string.Empty;

        public int PlannedQuantity { get; set; }

        public string MRPStatus { get; set; } = "Draft";

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<MaterialRequirementItem>
            MaterialItems
        { get; set; }
            = new List<MaterialRequirementItem>();
    }
}