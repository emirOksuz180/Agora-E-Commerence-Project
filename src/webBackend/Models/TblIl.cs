using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;

[Table("tbl_il")]
public partial class TblIl
{
    [Column("id")]
    public int Id { get; set; }
     
    [Column("ilAdi")]    
    public string IlAdi { get; set; } = null!;

    public virtual ICollection<TblIlce> TblIlces { get; set; } = new List<TblIlce>();
}
