using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class OrderStatus
{
    public int Id { get; set; }

    public string StatusKey { get; set; } = null!;

    public string StatusDisplayName { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
}
