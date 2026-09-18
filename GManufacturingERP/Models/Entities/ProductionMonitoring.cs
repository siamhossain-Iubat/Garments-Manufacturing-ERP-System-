namespace GManufacturingERP.Models.Entities
{
    public class ProductionMonitoring
    {
        public int Id { get; set; }

        public string MonitoringNumber { get; set; } = string.Empty;

        public string ProductionReference { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal PlannedQuantity { get; set; }

        public decimal ProducedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public decimal RemainingQuantity { get; set; }

        public string Unit { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public string Status { get; set; } = "Not Started";

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductionProgressEntry> ProgressEntries { get; set; }
            = new List<ProductionProgressEntry>();
        public int? ProductionPlanId { get; set; }

        public ProductionPlan? ProductionPlan { get; set; }
    }
}