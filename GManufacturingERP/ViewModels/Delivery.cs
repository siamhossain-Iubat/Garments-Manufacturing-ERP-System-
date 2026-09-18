using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GManufacturingERP.Models.Entities
{
    public class Delivery
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string DeliveryNumber { get; set; } = string.Empty;

        [Required]
        public int DispatchId { get; set; }

        public Dispatch Dispatch { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "PCS";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        public int TotalCartons { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(150)]
        public string ReceiverName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReceiverPhone { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? DeliveredBy { get; set; }

        public DateTime? DeliveredAt { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}