using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class PackingItem
    {
        public int Id { get; set; }


        // ==========================================
        // Packing
        // ==========================================

        [Required]
        public int PackingId { get; set; }

        public Packing Packing { get; set; } = null!;


        // ==========================================
        // Finished Goods
        // ==========================================

        [Required]
        public int FinishedGoodsStockId { get; set; }

        public FinishedGoodsStock FinishedGoodsStock { get; set; }
            = null!;


        // ==========================================
        // Product
        // ==========================================

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }
            = string.Empty;


        [Required]
        [StringLength(50)]
        public string Unit { get; set; }
            = "PCS";


        // ==========================================
        // Quantity
        // ==========================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }


        public int CartonNumber { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal QuantityPerCarton { get; set; }


        // ==========================================
        // Remarks
        // ==========================================

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}