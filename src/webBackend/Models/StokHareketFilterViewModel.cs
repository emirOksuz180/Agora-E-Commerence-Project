namespace webBackend.Models
{
    public class StokHareketFilterViewModel
    {
        public string? ProductName { get; set; }
        public string? LocationBarcode { get; set; }
        public string? MovementType { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}