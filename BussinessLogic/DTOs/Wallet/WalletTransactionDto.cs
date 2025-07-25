using System;

namespace BussinessLogic.DTOs.Wallet
{
    public class WalletTransactionDto
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
