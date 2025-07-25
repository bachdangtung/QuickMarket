using BussinessLogic.DTOs.Wallet;

namespace Services.Interfaces
{
    public interface IWalletService
    {
        // Wallet Operations
        Task<ServiceResult<WalletDto>> GetUserWalletAsync(int userId);
        
        // Transaction Operations
        Task<ServiceResult<IEnumerable<WalletTransactionDto>>> GetUserTransactionsAsync(int userId, int page = 1, int pageSize = 10);
        
        // Topup with VNPay
        Task<ServiceResult<string>> TopUpWithVNPayAsync(TopupRequestDto request, string ipAddress);
        Task<ServiceResult> CompleteVNPayTopUpAsync(VNPayResponse response);
        
        // Legacy VPPay methods (có thể xóa hoặc đánh dấu obsolete sau khi chuyển đổi hoàn toàn sang VNPay)
        Task<ServiceResult<string>> TopUpWithVPPayAsync(TopupRequestDto request);
        Task<ServiceResult> CompleteVPPayTopUpAsync(VPPayResponse response);
        
        // Withdraw
        Task<ServiceResult> WithdrawFundsAsync(WithdrawRequestDto request);
        
        // Internal transfers (for purchases)
        Task<ServiceResult> ProcessPurchasePaymentAsync(int buyerId, int sellerId, int productId, decimal amount);
    }
}
