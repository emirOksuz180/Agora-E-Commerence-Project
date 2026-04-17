using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class CarrierRegion
{
    public int Id { get; set; }

    public int CarrierId { get; set; }

    public int RegionId { get; set; }

    public virtual Carrier Carrier { get; set; } = null!;

    public virtual ShippingRegion Region { get; set; } = null!;
}
