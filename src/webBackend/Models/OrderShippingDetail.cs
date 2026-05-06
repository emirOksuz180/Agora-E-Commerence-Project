using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class OrderShippingDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CarrierId { get; set; }

    public decimal ShippingPrice { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
