namespace GManufacturingERP.Models.Entities
{
    public class MaterialIssueItem
    {
        public int Id { get; set; }

        public int MaterialIssueId { get; set; }

        public MaterialIssue MaterialIssue { get; set; } = null!;

        public int InventoryStockId { get; set; }

        public InventoryStock InventoryStock { get; set; } = null!;

        public string MaterialName { get; set; } = string.Empty;

        public string MaterialCategory { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal RequestedQuantity { get; set; }

        public decimal IssuedQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}