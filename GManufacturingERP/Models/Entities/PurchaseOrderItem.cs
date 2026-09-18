namespace GManufacturingERP.Models.Entities
{
    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder?
            PurchaseOrder
        { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string MaterialCategory { get; set; }
            = string.Empty;

        public string Unit { get; set; }
            = string.Empty;

        public decimal OrderedQuantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string? Remarks { get; set; }
    }
}