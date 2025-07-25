using BussinessLogic.Models;

namespace Repositories.Interfaces
{
    public interface IWalletRepository
    {
        // Wallet Operations
        Task<Wallet> GetWalletByUserIdAsync(int userId);
        Task<bool> CreateWalletAsync(Wallet wallet);
        Task<bool> UpdateWalletBalanceAsync(int userId, decimal amount);
        
        // Transaction Operations
        Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(int userId, int skip, int take);
        Task<int> GetUserTransactionCountAsync(int userId);
        Task<FinancialTransaction> GetTransactionByIdAsync(int transactionId);
        Task<FinancialTransaction> CreateTransactionAsync(FinancialTransaction transaction);
        Task<bool> UpdateTransactionStatusAsync(int transactionId, string status);
    }
}
