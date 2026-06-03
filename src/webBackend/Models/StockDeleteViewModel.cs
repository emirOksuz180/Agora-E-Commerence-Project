namespace webBackend.Models
{
    // Stok Silme için gelen veriyi karşılar
    public class StockDeleteViewModel
    {
        public int ProductId { get; set; }
        public string LocationBarcode { get; set; }
    }
}