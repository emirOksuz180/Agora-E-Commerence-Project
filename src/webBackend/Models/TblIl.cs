using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class TblIl
{
    public int Id { get; set; }

    public string IlAdi { get; set; } = null!;

    public virtual ICollection<TblIlce> TblIlces { get; set; } = new List<TblIlce>();
}
