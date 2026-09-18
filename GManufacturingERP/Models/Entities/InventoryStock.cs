namespace GManufacturingERP.Models.Entities
{
    public class InventoryStock
    {
        public int Id { get; set; }

        public string MaterialName { get; set; } = string.Empty;

        public string MaterialCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal AvailableQuantity { get; set; }

        public decimal ReservedQuantity { get; set; }

        public decimal ReorderLevel { get; set; }

        public string Location { get; set; } = "Main Store";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryTransaction> Transactions { get; set; }
            = new List<InventoryTransaction>();
    }
}