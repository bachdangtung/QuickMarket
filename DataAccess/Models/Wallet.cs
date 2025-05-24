using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Wallet
{
    public int UserId { get; set; }

    public decimal Balance { get; set; }

    public DateTime LastUpdate { get; set; }

    public virtual User User { get; set; } = null!;
}
