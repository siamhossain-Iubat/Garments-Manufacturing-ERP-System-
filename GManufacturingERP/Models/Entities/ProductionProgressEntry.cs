namespace GManufacturingERP.Models.Entities
{
    public class ProductionProgressEntry
    {
        public int Id { get; set; }

        public int ProductionMonitoringId { get; set; }

        public ProductionMonitoring ProductionMonitoring { get; set; }
            = null!;

        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        public decimal ProducedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public decimal TotalProducedAfterEntry { get; set; }

        public decimal RemainingQuantityAfterEntry { get; set; }

        public string? Remarks { get; set; }

        public string? EnteredBy { get; set; }
    }
}