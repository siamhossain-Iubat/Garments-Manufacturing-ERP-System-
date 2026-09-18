using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class CreateGoodsReceiptViewModel
    {
        public int PurchaseOrderId { get; set; }

        public string PurchaseOrderNumber { get; set; }
            = string.Empty;

        [Required(ErrorMessage = "Receipt date is required.")]
        [DataType(DataType.Date)]
        public DateTime ReceiptDate { get; set; }

        [Display(Name = "Delivery Challan Number")]
        public string? DeliveryChallanNumber { get; set; }

        public string? Remarks { get; set; }

        public List<GoodsReceiptItemViewModel> Items { get; set; }
            = new List<GoodsReceiptItemViewModel>();
    }

    public class GoodsReceiptItemViewModel
    {
        [Required]
        public string MaterialName { get; set; }
            = string.Empty;

        public string? MaterialCategory { get; set; }

        public string? Unit { get; set; }

        [Display(Name = "Ordered Quantity")]
        [Range(0, double.MaxValue)]
        public decimal OrderedQuantity { get; set; }

        [Required]
        [Display(Name = "Received Quantity")]
        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Received quantity cannot be negative.")]
        public decimal ReceivedQuantity { get; set; }

        [Required]
        [Display(Name = "Accepted Quantity")]
        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Accepted quantity cannot be negative.")]
        public decimal AcceptedQuantity { get; set; }

        [Required]
        [Display(Name = "Rejected Quantity")]
        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Rejected quantity cannot be negative.")]
        public decimal RejectedQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}