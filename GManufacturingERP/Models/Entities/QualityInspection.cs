namespace GManufacturingERP.Models.Entities
{
    public class QualityInspection
    {
        public int Id { get; set; }

        public string InspectionNumber { get; set; } = string.Empty;

        public int ProductionMonitoringId { get; set; }

        public ProductionMonitoring ProductionMonitoring { get; set; }
            = null!;

        public string ProductionReference { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal ProducedQuantity { get; set; }

        public decimal InspectedQuantity { get; set; }

        public decimal PassedQuantity { get; set; }

        public decimal FailedQuantity { get; set; }

        public string InspectionResult { get; set; } = "Pending";

        public string Status { get; set; } = "Draft";

        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

        public string? InspectedBy { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? DefectDescription { get; set; }

        public string? Remarks { get; set; }
    }
}