using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Message
{
    public int MessId { get; set; }

    public int FromUserId { get; set; }

    public int ToUserId { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTime SentTime { get; set; }

    public virtual User FromUser { get; set; } = null!;

    public virtual User ToUser { get; set; } = null!;
}
