using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Stock
{
    public int StockId { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int DamagedQuantity { get; set; }

    public decimal DailyStorageCostPerUnit { get; set; }

    public virtual WarehouseLocation Location { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
