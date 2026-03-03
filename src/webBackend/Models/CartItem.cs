using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int UrunId { get; set; }

    public int CartId { get; set; }

    public int Miktar { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Urun { get; set; } = null!;
}
