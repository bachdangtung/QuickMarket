namespace BussinessLogic.DTOs.Wallet
{
    public class VNPayRequest
    {
        public string vnp_Version { get; set; } = null!;
        public string vnp_Command { get; set; } = "pay";
        public string vnp_TmnCode { get; set; } = null!;
        public string vnp_Amount { get; set; } = null!;  // Số tiền * 100
        public string vnp_CreateDate { get; set; } = null!;
        public string vnp_CurrCode { get; set; } = "VND";
        public string vnp_IpAddr { get; set; } = null!;
        public string vnp_Locale { get; set; } = "vn";
        public string vnp_OrderInfo { get; set; } = null!;
        public string vnp_OrderType { get; set; } = "250000"; // Thanh toán hóa đơn
        public string vnp_ReturnUrl { get; set; } = null!;
        public string vnp_TxnRef { get; set; } = null!;  // Mã giao dịch unique
        public string? vnp_BankCode { get; set; }
        public string? vnp_SecureHash { get; set; }
    }
}
