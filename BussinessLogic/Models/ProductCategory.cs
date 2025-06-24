using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class ProductCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string? StatusId { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
