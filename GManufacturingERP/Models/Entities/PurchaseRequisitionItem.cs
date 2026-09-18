namespace GManufacturingERP.Models.Entities
{
    public class PurchaseRequisitionItem
    {
        public int Id { get; set; }

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisition?
            PurchaseRequisition
        { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string MaterialCategory { get; set; }
            = string.Empty;

        public string Unit { get; set; }
            = string.Empty;

        public decimal RequestedQuantity { get; set; }

        public decimal EstimatedUnitPrice { get; set; }

        public decimal EstimatedTotalPrice { get; set; }

        public string? Remarks { get; set; }
    }
}