using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public int? ProductId { get; set; }

    public int? UserId { get; set; }

    public string? Comment { get; set; }

    public DateTime ReviewDate { get; set; }

    public int? ThreadId { get; set; }

    public virtual ICollection<ProductReview> InverseThread { get; set; } = new List<ProductReview>();

    public virtual Product? Product { get; set; }

    public virtual ProductReview? Thread { get; set; }

    public virtual User? User { get; set; }
}
