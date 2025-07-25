namespace BussinessLogic.DTOs.Wallet
{
    public class WithdrawRequestDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountName { get; set; } = null!;
    }
}
