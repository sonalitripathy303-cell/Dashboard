namespace LoginToDashboard.Models
{
    public class ReportRowModel
    {
        public string ItemName { get; set; }
        public DateTime? SoldDate { get; set; }
        public int SoldQuantity { get; set; }
        public decimal SoldPrice { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal RemainingPrice { get; set; }
    }
}
