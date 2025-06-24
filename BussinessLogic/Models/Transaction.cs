using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int? BuyerId { get; set; }

    public int? SellerId { get; set; }

    public int? ProductId { get; set; }

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public virtual User? Buyer { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? Seller { get; set; }

    public virtual UserShipped? UserShipped { get; set; }
}
