namespace webBackend.Models
{
   
    public class StockGroupViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalAvailableQuantity { get; set; }
        public List<LocationDetailViewModel> Locations { get; set; } = new();
    }

    // Accordion'ın açılan içeriği (Lokasyon bazlı)
    public class LocationDetailViewModel
    {
        public string LocationBarcode { get; set; }
        public int Quantity { get; set; }
    }

    // SP'den ham veriyi karşılamak için
    public class RawStockResult
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string LocationBarcode { get; set; }
        public int AvailableQuantity { get; set; }
    }
}