using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class FinancialTransaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public string TransactionType { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public virtual User User { get; set; } = null!;
}
