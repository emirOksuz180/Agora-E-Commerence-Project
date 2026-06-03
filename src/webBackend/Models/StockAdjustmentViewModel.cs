namespace webBackend.Models
{
    // Stok Güncelleme için gelen veriyi karşılar
    public class StockAdjustmentViewModel
    {
        public int ProductId { get; set; }
        public string LocationBarcode { get; set; }
        public int NewQuantity { get; set; }
    }
}