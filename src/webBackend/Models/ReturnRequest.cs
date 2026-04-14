using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class ReturnRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime? RequestDate { get; set; }

    public int? CurrentStatusId { get; set; }

    public bool? IsRefunded { get; set; }

    public virtual OrderStatus? CurrentStatus { get; set; }
}
