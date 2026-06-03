using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class WarehouseLocation
{
    public int LocationId { get; set; }

    public int WarehouseId { get; set; }

    public string ZoneCode { get; set; } = null!;

    public string Aisle { get; set; } = null!;

    public string Shelf { get; set; } = null!;

    public string Bin { get; set; } = null!;

    public string? LocationBarcode { get; set; }

    public decimal MaxVolumeDesi { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
