using System;

namespace BussinessLogic.DTOs.Wallet
{
    public class WalletDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
