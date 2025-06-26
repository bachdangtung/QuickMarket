using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class ExternalLogin
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public DateTime DateCreated { get; set; }

    public virtual User User { get; set; } = null!;
}
