namespace GManufacturingERP.Models.Entities
{
    public class ProductionPlan
    {
        public int Id { get; set; }

        public string PlanNumber { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        public string ProductDescription { get; set; } = string.Empty;

        public string BuyerCompany { get; set; } = string.Empty;

        public int PlannedQuantity { get; set; }

        public DateTime PlannedStartDate { get; set; }

        public DateTime ExpectedCompletionDate { get; set; }

        // PendingApproval / Planned / Rejected /
        // InProgress / Completed
        public string ProductionStatus { get; set; } = "PendingApproval";

        public string? ProductionRemarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<MaterialIssue> MaterialIssues { get; set; }
            = new List<MaterialIssue>();

        public ICollection<ProductionMonitoring> ProductionMonitorings { get; set; }
            = new List<ProductionMonitoring>();
    }
}