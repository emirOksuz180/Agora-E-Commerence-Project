using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class TblIlce
{
    public int Id { get; set; }

    public int IlId { get; set; }

    public string IlceAdi { get; set; } = null!;

    public virtual TblIl Il { get; set; } = null!;
}
