using GManufacturingERP.Models.Entities;

namespace GManufacturingERP.ViewModels
{
    public class SalesDashboardViewModel
    {
        public int TotalBuyers { get; set; }

        public int PendingBuyers { get; set; }

        public int ApprovedBuyers { get; set; }

        public List<ApplicationUser> RecentBuyers { get; set; }
            = new List<ApplicationUser>();
    }
}