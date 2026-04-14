using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class ShippingRate
{
    public int Id { get; set; }

    public int? CarrierId { get; set; }

    public int? RegionId { get; set; }

    public decimal MinDesi { get; set; }

    public decimal MaxDesi { get; set; }

    public decimal Price { get; set; }

    public decimal? ExtraDesiPrice { get; set; }

    public virtual Carrier? Carrier { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ShippingRegion? Region { get; set; }
}
