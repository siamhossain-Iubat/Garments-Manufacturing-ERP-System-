using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int DeliveryId { get; set; }

        public Delivery Delivery { get; set; } = null!;

        public int? SalesOrderId { get; set; }

        public SalesOrder? SalesOrder { get; set; }

        [Required]
        [StringLength(200)]
        public string BuyerName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DueAmount { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? IssuedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}