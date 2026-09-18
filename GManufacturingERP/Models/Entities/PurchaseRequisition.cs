namespace GManufacturingERP.Models.Entities
{
    public class PurchaseRequisition
    {
        public int Id { get; set; }

        public string RequisitionNumber { get; set; }
            = string.Empty;

        public int MaterialRequirementPlanId { get; set; }

        public MaterialRequirementPlan?
            MaterialRequirementPlan
        { get; set; }

        public string RequestedBy { get; set; }
            = string.Empty;

        public DateTime RequiredDate { get; set; }

        public string Priority { get; set; } = "Normal";

        public string RequisitionStatus { get; set; }
            = "Draft";

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public ICollection<PurchaseRequisitionItem>
            Items
        { get; set; }
            = new List<PurchaseRequisitionItem>();
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? RejectionReason { get; set; }
    }
}