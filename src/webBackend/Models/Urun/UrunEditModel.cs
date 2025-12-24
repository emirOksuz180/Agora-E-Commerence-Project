using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace webBackend.Models;

public partial class UrunEditModel: UrunModel
{
    [Key]
    public int ProductId { get; set; }
    
    

    [InverseProperty("Product")]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    
}
