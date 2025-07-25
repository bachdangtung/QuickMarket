namespace BussinessLogic.DTOs.Wallet
{
    public class VNPayResponse
    {
        public string? vnp_Amount { get; set; }
        public string? vnp_BankCode { get; set; }
        public string? vnp_BankTranNo { get; set; }
        public string? vnp_CardType { get; set; }
        public string? vnp_OrderInfo { get; set; }
        public string? vnp_PayDate { get; set; }
        public string? vnp_ResponseCode { get; set; }
        public string? vnp_TmnCode { get; set; }
        public string? vnp_TransactionNo { get; set; }
        public string? vnp_TransactionStatus { get; set; }
        public string? vnp_TxnRef { get; set; }
        public string? vnp_SecureHash { get; set; }

        // Tiện ích
        public bool IsSuccess => vnp_ResponseCode == "00" && vnp_TransactionStatus == "00";
        public decimal Amount => string.IsNullOrEmpty(vnp_Amount) ? 0 : Convert.ToDecimal(vnp_Amount) / 100;
    }
}
