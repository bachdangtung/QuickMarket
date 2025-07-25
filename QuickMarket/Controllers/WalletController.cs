using BussinessLogic.DTOs.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickMarket.Extensions;
using Services.Interfaces;

namespace QuickMarket.Controllers
{
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var result = await _walletService.GetUserWalletAsync(userId);
            
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index", "Home");
            }

            var transactionsResult = await _walletService.GetUserTransactionsAsync(userId);
            
            ViewBag.Transactions = transactionsResult.Success ? transactionsResult.Data : new List<WalletTransactionDto>();
            
            return View(result.Data);
        }

        [Authorize]
        public IActionResult TopUp()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUp(TopupRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            request.UserId = User.GetUserId();
            // Sử dụng URL từ cấu hình trong appsettings.json thay vì tạo động
            request.ReturnUrl = "https://d94af6116a67.ngrok-free.app/Wallet/VNPayReturn";
            request.PaymentMethod = "VNPay"; // Mặc định sử dụng VNPAY
            
            // Lấy địa chỉ IP của khách hàng (cần thiết cho VNPAY)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            
            // Debug info
            Console.WriteLine($"Debug: TopUp request - UserId: {request.UserId}, Amount: {request.Amount}, ReturnUrl: {request.ReturnUrl}");
            
            // Sử dụng VNPAY để tạo URL thanh toán
            var result = await _walletService.TopUpWithVNPayAsync(request, ipAddress);
            
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                Console.WriteLine($"Debug: TopUp failed - {result.Message}");
                return View(request);
            }
            
            Console.WriteLine($"Debug: TopUp success - Redirect to: {result.Data}");
            
            // Redirect đến cổng thanh toán VNPAY
            return Redirect(result.Data);
        }

        [Authorize]
        public async Task<IActionResult> VNPayReturn([FromQuery] VNPayResponse response)
        {
            var result = await _walletService.CompleteVNPayTopUpAsync(response);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Nạp tiền thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
            
            return RedirectToAction("Index");
        }
        
        // Phương thức cũ cho VPPay (có thể xóa khi chuyển hoàn toàn sang VNPAY)
        [Authorize]
        public async Task<IActionResult> TopUpComplete(VPPayResponse response)
        {
            var result = await _walletService.CompleteVPPayTopUpAsync(response);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Nạp tiền thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
            
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Withdraw()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            request.UserId = User.GetUserId();
            
            var result = await _walletService.WithdrawFundsAsync(request);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Yêu cầu rút tiền đã được gửi và đang được xử lý.";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = result.Message;
            return View(request);
        }

        [Authorize]
        public async Task<IActionResult> Transactions(int page = 1)
        {
            var userId = User.GetUserId();
            var pageSize = 20;
            
            var result = await _walletService.GetUserTransactionsAsync(userId, page, pageSize);
            
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }
            
            return View(result.Data);
        }
    }
}
