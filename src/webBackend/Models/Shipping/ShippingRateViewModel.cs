using System.Collections.Generic;
using webBackend.Models;

namespace webBackend.Models.Shipping // Bu satır kritik!
{
    public class ShippingRateViewModel
    {
        public int CarrierId { get; set; }
        public string? CarrierName { get; set; } 
        public List<ShippingRegion> Regions { get; set; } = new();
        public List<ShippingRate> ExistingRates { get; set; } = new();
    }
}