using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;

[Table("OrderItem")]
public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int UrunId { get; set; }

    public double Fiyat { get; set; }

    public int Miktar { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Urun { get; set; } = null!;
}
