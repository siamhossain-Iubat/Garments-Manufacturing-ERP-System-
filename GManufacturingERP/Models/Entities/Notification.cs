using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.Models.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        // User who will receive the notification
        [Required]
        public string RecipientUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Example:
        // Info
        // PartialShipment
        // Approval
        [StringLength(50)]
        public string Type { get; set; } = "Info";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional link with partial shipment request
        public int? PartialShipmentRequestId { get; set; }

        public PartialShipmentRequest? PartialShipmentRequest { get; set; }
    }
}