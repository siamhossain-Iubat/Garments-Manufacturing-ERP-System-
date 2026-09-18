using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class GoodsReceipt
    {
        public int Id { get; set; }

        [Required]
        public string GRNNumber { get; set; } = string.Empty;

        public int PurchaseOrderId { get; set; }

        public PurchaseOrder? PurchaseOrder { get; set; }

        [Required]
        public string ReceivedBy { get; set; } = string.Empty;

        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        public string ReceiptStatus { get; set; } = "Draft";

        public string? DeliveryChallanNumber { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GoodsReceiptItem> Items { get; set; }
            = new List<GoodsReceiptItem>();
    }
}