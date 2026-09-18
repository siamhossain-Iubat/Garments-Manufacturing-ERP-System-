using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class PurchaseRequisitionApprovalViewModel
    {
        public int Id { get; set; }

        public string RequisitionNumber { get; set; }
            = string.Empty;

        public string? RejectionReason { get; set; }
    }
}