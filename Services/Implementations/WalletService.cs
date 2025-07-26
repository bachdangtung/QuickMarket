using AutoMapper;
using BussinessLogic.DTOs.Wallet;
using BussinessLogic.Models;
using BussinessLogic.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly VNPayService _vnPayService;

        public WalletService(
            IWalletRepository walletRepository,
            IUserRepository userRepository,
            IMapper mapper,
            VNPayService vnPayService)
        {
            _walletRepository = walletRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _vnPayService = vnPayService;
        }

        public async Task<ServiceResult<WalletDto>> GetUserWalletAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                    return ServiceResult<WalletDto>.ErrorResult("Không tìm thấy người dùng.");

                var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
                if (wallet == null)
                {
                    // Tự động tạo ví nếu chưa có
                    wallet = new Wallet
                    {
                        UserId = userId,
                        Balance = 0,
                        LastUpdate = DateTime.Now
                    };
                    await _walletRepository.CreateWalletAsync(wallet);
                }

                var walletDto = _mapper.Map<WalletDto>(wallet);
                walletDto.Username = user.Username;

                return ServiceResult<WalletDto>.SuccessResult(walletDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<WalletDto>.ErrorResult($"Lỗi khi truy vấn ví: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<WalletTransactionDto>>> GetUserTransactionsAsync(int userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var skip = (page - 1) * pageSize;
                var transactions = await _walletRepository.GetUserTransactionsAsync(userId, skip, pageSize);
                
                var transactionDtos = _mapper.Map<IEnumerable<WalletTransactionDto>>(transactions);
                
                // Lấy tên người dùng
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user != null)
                {
                    foreach (var dto in transactionDtos)
                    {
                        dto.Username = user.Username;
                    }
                }
                
                return ServiceResult<IEnumerable<WalletTransactionDto>>.SuccessResult(transactionDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<WalletTransactionDto>>.ErrorResult($"Lỗi khi truy vấn giao dịch: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> TopUpWithVNPayAsync(TopupRequestDto request, string ipAddress)
        {
            try
            {
                // Validate request
                if (request.Amount <= 0)
                    return ServiceResult<string>.ErrorResult("Số tiền phải lớn hơn 0.");

                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user == null)
                    return ServiceResult<string>.ErrorResult("Không tìm thấy người dùng.");

                // Create pending transaction
                var orderId = $"TOP{DateTime.Now.Ticks}";
                var transaction = new FinancialTransaction
                {
                    UserId = request.UserId,
                    TransactionType = TransactionType.Deposit.ToString(),
                    Amount = request.Amount,
                    TransactionDate = DateTime.Now,
                    Status = TransactionStatus.Pending.ToString(),
                    Description = $"Nạp tiền qua VNPAY - Mã giao dịch: {orderId}"
                };

                var createdTransaction = await _walletRepository.CreateTransactionAsync(transaction);

                // Generate payment URL
                var paymentUrl = _vnPayService.CreatePaymentUrl(request, orderId, ipAddress);
                
                return ServiceResult<string>.SuccessResult(paymentUrl, "Tạo yêu cầu nạp tiền thành công");
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.ErrorResult($"Lỗi khi tạo yêu cầu nạp tiền: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CompleteVNPayTopUpAsync(VNPayResponse response)
        {
            try
            {
                // Validate response
                if (!_vnPayService.ValidateCallback(response))
                {
                    return ServiceResult.ErrorResult("Chữ ký không hợp lệ. Thanh toán có thể bị giả mạo.");
                }

                if (!response.IsSuccess)
                {
                    return ServiceResult.ErrorResult($"Thanh toán thất bại: {response.vnp_ResponseCode}");
                }

                // Tìm giao dịch theo TxnRef trong description
                var transactions = await _walletRepository.GetUserTransactionsAsync(0, 0, 100);
                var transaction = transactions.FirstOrDefault(t => 
                    t.Description != null && 
                    response.vnp_TxnRef != null &&
                    t.Description.Contains(response.vnp_TxnRef) && 
                    t.Status == TransactionStatus.Pending.ToString());

                if (transaction == null)
                {
                    return ServiceResult.ErrorResult("Không tìm thấy giao dịch nạp tiền tương ứng.");
                }

                // Cập nhật trạng thái giao dịch
                await _walletRepository.UpdateTransactionStatusAsync(
                    transaction.TransactionId, TransactionStatus.Completed.ToString());

                // Cập nhật số dư ví
                await _walletRepository.UpdateWalletBalanceAsync(transaction.UserId, transaction.Amount);

                return ServiceResult.SuccessResult("Nạp tiền thành công");
            }
            catch (Exception ex)
            {
                return ServiceResult.ErrorResult($"Lỗi khi hoàn tất nạp tiền: {ex.Message}");
            }
        }

        public async Task<ServiceResult> WithdrawFundsAsync(WithdrawRequestDto request)
        {
            try
            {
                // Validate request
                if (request.Amount <= 0)
                    return ServiceResult.ErrorResult("Số tiền phải lớn hơn 0.");

                var wallet = await _walletRepository.GetWalletByUserIdAsync(request.UserId);
                if (wallet == null)
                    return ServiceResult.ErrorResult("Không tìm thấy ví.");

                if (wallet.Balance < request.Amount)
                    return ServiceResult.ErrorResult("Số dư không đủ để thực hiện giao dịch.");

                // Create withdrawal transaction
                var transaction = new FinancialTransaction
                {
                    UserId = request.UserId,
                    TransactionType = TransactionType.Withdrawal.ToString(),
                    Amount = -request.Amount, // Negative amount for withdrawal
                    TransactionDate = DateTime.Now,
                    Status = TransactionStatus.Pending.ToString(),
                    Description = $"Rút tiền về tài khoản {request.BankName}: {request.AccountNumber} - {request.AccountName}"
                };

                await _walletRepository.CreateTransactionAsync(transaction);

                // In a real application, this would be processed asynchronously
                // For simplicity, we'll process it immediately
                await _walletRepository.UpdateWalletBalanceAsync(request.UserId, -request.Amount);
                await _walletRepository.UpdateTransactionStatusAsync(
                    transaction.TransactionId, TransactionStatus.Completed.ToString());

                return ServiceResult.SuccessResult("Yêu cầu rút tiền đã được tạo và đang được xử lý");
            }
            catch (Exception ex)
            {
                return ServiceResult.ErrorResult($"Lỗi khi tạo yêu cầu rút tiền: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ProcessPurchasePaymentAsync(int buyerId, int sellerId, int productId, decimal amount)
        {
            try
            {
                var buyerWallet = await _walletRepository.GetWalletByUserIdAsync(buyerId);
                if (buyerWallet == null)
                    return ServiceResult.ErrorResult("Không tìm thấy ví người mua.");

                if (buyerWallet.Balance < amount)
                    return ServiceResult.ErrorResult("Số dư không đủ để thực hiện thanh toán.");

                var sellerWallet = await _walletRepository.GetWalletByUserIdAsync(sellerId);
                if (sellerWallet == null)
                {
                    // Tự động tạo ví cho người bán nếu chưa có
                    sellerWallet = new Wallet
                    {
                        UserId = sellerId,
                        Balance = 0,
                        LastUpdate = DateTime.Now
                    };
                    await _walletRepository.CreateWalletAsync(sellerWallet);
                }

                // Trừ tiền người mua
                var buyerTransaction = new FinancialTransaction
                {
                    UserId = buyerId,
                    TransactionType = TransactionType.Purchase.ToString(),
                    Amount = -amount,
                    TransactionDate = DateTime.Now,
                    Status = TransactionStatus.Completed.ToString(),
                    Description = $"Thanh toán cho sản phẩm ID: {productId}"
                };
                await _walletRepository.CreateTransactionAsync(buyerTransaction);
                await _walletRepository.UpdateWalletBalanceAsync(buyerId, -amount);

                // Cộng tiền người bán
                var sellerTransaction = new FinancialTransaction
                {
                    UserId = sellerId,
                    TransactionType = TransactionType.Sale.ToString(),
                    Amount = amount,
                    TransactionDate = DateTime.Now,
                    Status = TransactionStatus.Completed.ToString(),
                    Description = $"Nhận tiền từ bán sản phẩm ID: {productId}"
                };
                await _walletRepository.CreateTransactionAsync(sellerTransaction);
                await _walletRepository.UpdateWalletBalanceAsync(sellerId, amount);

                return ServiceResult.SuccessResult("Thanh toán thành công");
            }
            catch (Exception ex)
            {
                return ServiceResult.ErrorResult($"Lỗi khi xử lý thanh toán: {ex.Message}");
            }
        }
    }
}
