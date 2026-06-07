namespace webBackend.Models
{
    public class StokHareketViewModel
    {
        
        public string ProductName { get; set; } = string.Empty;
        public string LocationBarcode { get; set; } = string.Empty;

        
        public string MovementType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public DateTime MovementDate { get; set; }
        public string PerformedBy { get; set; } = "Sistem";
    }
}