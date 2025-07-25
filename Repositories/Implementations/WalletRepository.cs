using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class WalletRepository : IWalletRepository
    {
        private readonly QuickMarketContext _context;

        public WalletRepository(QuickMarketContext context)
        {
            _context = context;
        }

        public async Task<Wallet> GetWalletByUserIdAsync(int userId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<bool> CreateWalletAsync(Wallet wallet)
        {
            _context.Wallets.Add(wallet);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateWalletBalanceAsync(int userId, decimal amount)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            
            if (wallet == null)
                return false;
                
            wallet.Balance += amount;
            wallet.LastUpdate = DateTime.Now;
            
            _context.Wallets.Update(wallet);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(int userId, int skip, int take)
        {
            return await _context.FinancialTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUserTransactionCountAsync(int userId)
        {
            return await _context.FinancialTransactions
                .CountAsync(t => t.UserId == userId);
        }

        public async Task<FinancialTransaction> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.FinancialTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<FinancialTransaction> CreateTransactionAsync(FinancialTransaction transaction)
        {
            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, string status)
        {
            var transaction = await _context.FinancialTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
                
            if (transaction == null)
                return false;
                
            transaction.Status = status;
            
            _context.FinancialTransactions.Update(transaction);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
