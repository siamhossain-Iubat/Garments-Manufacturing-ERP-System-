namespace GManufacturingERP.Models.Entities
{
    public class MaterialIssue
    {
        public int Id { get; set; }

        public string IssueNumber { get; set; } = string.Empty;

        public int? MaterialRequirementPlanId { get; set; }

        public MaterialRequirementPlan? MaterialRequirementPlan { get; set; }

        public string ProductionReference { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Draft";

        public string? RequestedBy { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? Remarks { get; set; }

        public ICollection<MaterialIssueItem> Items { get; set; }
            = new List<MaterialIssueItem>();
    }
}