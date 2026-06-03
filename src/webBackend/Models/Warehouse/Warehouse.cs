using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string? Location { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<WarehouseLocation> WarehouseLocations { get; set; } = new List<WarehouseLocation>();
}
