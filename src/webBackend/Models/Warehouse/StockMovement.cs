using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class StockMovement
{
    public int MovementId { get; set; }

    public int StockId { get; set; }

    public string MovementType { get; set; } = null!;

    public int QuantityChange { get; set; }

    public int? RelatedReferenceId { get; set; }

    public DateTime? MovementDate { get; set; }

    public string? PerformedBy { get; set; }

    public virtual Stock Stock { get; set; } = null!;
}
