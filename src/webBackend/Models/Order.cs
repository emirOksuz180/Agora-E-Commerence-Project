using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
