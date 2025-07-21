using System;
using System.Collections.Generic;

namespace BussinessLogic.Models;

public partial class ChatProduct
{
    public int Id { get; set; }
    
    public int UserId1 { get; set; }
    
    public int UserId2 { get; set; }
    
    public int ProductId { get; set; }
    
    public DateTime AddedDate { get; set; }
    
    public virtual User User1 { get; set; } = null!;
    
    public virtual User User2 { get; set; } = null!;
    
    public virtual Product Product { get; set; } = null!;
}
