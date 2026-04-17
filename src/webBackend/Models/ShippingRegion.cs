using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class ShippingRegion
{
    public int Id { get; set; }

    public string RegionName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<ShippingRate> ShippingRates { get; set; } = new List<ShippingRate>();

    public virtual ICollection<CarrierRegion> CarrierRegions { get; set; } = new List<CarrierRegion>();
}
