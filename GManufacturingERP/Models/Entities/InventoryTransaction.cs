namespace GManufacturingERP.Models.Entities
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int InventoryStockId { get; set; }

        public InventoryStock InventoryStock { get; set; } = null!;

        public string TransactionType { get; set; } = string.Empty;

        public string ReferenceNumber { get; set; } = string.Empty;

        public decimal QuantityIn { get; set; }

        public decimal QuantityOut { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public string? PerformedBy { get; set; }

        public string? Remarks { get; set; }
    }
}