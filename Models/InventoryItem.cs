namespace LoginToDashboard.Models
{
    public class InventoryItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int InitialQuantity { get; set; }
        public int SoldQuantity { get; set; }
        public decimal PricePerItem { get; set; }

        // Calculated Properties for presentation
        public int RemainingQuantity => InitialQuantity - SoldQuantity;
        public decimal RemainingPrice => RemainingQuantity * PricePerItem;
    }
}
