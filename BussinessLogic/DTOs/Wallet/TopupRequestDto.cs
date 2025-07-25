namespace BussinessLogic.DTOs.Wallet
{
    public class TopupRequestDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!; // "VPPay", "CreditCard", etc.
        public string ReturnUrl { get; set; } = null!;
    }
}
