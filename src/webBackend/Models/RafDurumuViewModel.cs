namespace webBackend.Models
{
    public class RafDurumuViewModel
    {
        public string LocationBarcode { get; set; }
        public decimal MaxVolumeDesi { get; set; }
        public decimal CurrentVolume { get; set; }
        public int TotalItems { get; set; } // Raftaki toplam ürün adedi
        
        // Doluluk oranını otomatik hesaplayan property
        public decimal OccupancyRate => MaxVolumeDesi > 0 ? (CurrentVolume / MaxVolumeDesi) * 100 : 0;
    }
}