using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public string CustomerId { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
