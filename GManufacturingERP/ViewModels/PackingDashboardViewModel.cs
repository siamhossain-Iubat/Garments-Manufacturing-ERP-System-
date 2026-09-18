namespace GManufacturingERP.ViewModels
{
    public class PackingDashboardViewModel
    {
        public int FinishedGoodsStockId { get; set; }

        public int? SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; }
            = string.Empty;

        public string ProductName { get; set; }
            = string.Empty;

        public string Unit { get; set; }
            = "PCS";


        public decimal OrderedQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

        public decimal AlreadyPackedQuantity { get; set; }

        public decimal RemainingOrderQuantity { get; set; }

        public decimal QuantityAvailableForPacking { get; set; }


        public bool IsFullyAvailable { get; set; }

        public bool IsPartiallyAvailable { get; set; }

        public bool IsWaitingForStock { get; set; }

        public int? PartialShipmentRequestId { get; set; }

        public string? PartialShipmentStatus { get; set; }
    }
}