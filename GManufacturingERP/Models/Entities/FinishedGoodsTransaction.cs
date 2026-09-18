using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class FinishedGoodsTransaction
    {
        public int Id { get; set; }

        public int FinishedGoodsStockId { get; set; }

        public FinishedGoodsStock FinishedGoodsStock { get; set; }
            = null!;

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal QuantityIn { get; set; }

        [Range(0, double.MaxValue)]
        public decimal QuantityOut { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? PerformedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}