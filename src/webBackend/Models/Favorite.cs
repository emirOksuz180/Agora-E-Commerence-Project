using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Favorite
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product? Product { get; set; }
}
