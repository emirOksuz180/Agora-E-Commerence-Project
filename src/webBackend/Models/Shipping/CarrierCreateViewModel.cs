using webBackend.Models.Shipping;

namespace webBackend.Models.Shipping;
public class CarrierCreateViewModel
{
    public Carrier Carrier { get; set; } = new Carrier();
    public List<ShippingRegion> AllRegions { get; set; } = new List<ShippingRegion>();
    public List<int> SelectedRegionIds { get; set; } = new List<int>(); // Seçilen bölgelerin ID'leri
}