using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class GoodsReceiptItem
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }

        public GoodsReceipt? GoodsReceipt { get; set; }

        [Required]
        public string MaterialName { get; set; } = string.Empty;

        public string? MaterialCategory { get; set; }

        public string? Unit { get; set; }

        public decimal OrderedQuantity { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public decimal AcceptedQuantity { get; set; }

        public decimal RejectedQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}