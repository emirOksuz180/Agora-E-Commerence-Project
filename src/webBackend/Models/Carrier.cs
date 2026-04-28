using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Carrier
{
    public int Id { get; set; }

    public string CarrierName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<ShippingRate> ShippingRates { get; set; } = new List<ShippingRate>();

    // Bir kargo firmasının birden fazla hizmet bölgesi ve fiyatı olabilir
    public virtual ICollection<CarrierRegion> CarrierRegions { get; set; } = new List<CarrierRegion>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

}
