using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class Favorite
{
    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime DateAdded { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
