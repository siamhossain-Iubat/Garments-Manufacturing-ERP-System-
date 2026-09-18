namespace GManufacturingERP.Models.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        public string PurchaseOrderNumber { get; set; }
            = string.Empty;

        public int PurchaseRequisitionId { get; set; }

        public PurchaseRequisition?
            PurchaseRequisition
        { get; set; }

        public string? SupplierName { get; set; }

        public string? SupplierContact { get; set; }

        public string? SupplierAddress { get; set; }

        public DateTime OrderDate { get; set; }
            = DateTime.UtcNow;

        public DateTime ExpectedDeliveryDate { get; set; }

        public string PaymentTerms { get; set; }
            = "Cash";

        public string DeliveryTerms { get; set; }
            = "Factory Delivery";

        public string PurchaseOrderStatus { get; set; }
            = "Draft";

        public decimal SubTotal { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<PurchaseOrderItem> Items { get; set; }
            = new List<PurchaseOrderItem>();
    }
}