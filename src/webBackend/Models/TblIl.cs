using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;



namespace webBackend.Models;

[Table("tbl_il")]
public partial class TblIl
{
    public int Id { get; set; }

    public string IlAdi { get; set; } = null!;

    public int? RegionId { get; set; }

    public virtual ShippingRegion? Region { get; set; }

    public virtual ICollection<TblIlce> TblIlces { get; set; } = new List<TblIlce>();
}

