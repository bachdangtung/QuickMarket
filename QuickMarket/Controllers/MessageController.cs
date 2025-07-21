using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using Services.Interfaces;
using BussinessLogic.DTOs.Messages;
using BussinessLogic.DTOs.Products;
using System;

namespace QuickMarket.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IProductService _productService;

        public MessageController(IMessageService messageService, IProductService productService)
        {
            _messageService = messageService;
            _productService = productService;
        }

        public async Task<IActionResult> Index(int? userId, int? productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var currentUserId = int.Parse(userIdClaim.Value);
            
            // If userId is provided, show chat with that user
            if (userId.HasValue)
            {
                // If productId is provided, attach it to the chat
                if (productId.HasValue)
                {
                    await _messageService.AttachProductToChat(currentUserId, userId.Value, productId.Value);
                    
                    // Generate an automatic message about the product
                    var product = await _productService.GetProductByIdAsync(productId.Value);
                    if (product != null)
                    {
                        string message = $"Tôi muốn hỏi về sản phẩm: {product.Name}";
                        await _messageService.SendMessage(currentUserId, userId.Value, message);
                    }
                }
                
                var chatHistory = await _messageService.GetChatHistory(currentUserId, userId.Value);
                return View(chatHistory);
            }
            
            // Otherwise, show all chats
            var allChats = await _messageService.GetAllChats(currentUserId);
            return View("AllChats", allChats);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int toUserId, string message)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var currentUserId = int.Parse(userIdClaim.Value);
            var result = await _messageService.SendMessage(currentUserId, toUserId, message);
            
            if (result)
            {
                return Json(new { success = true });
            }
            
            return Json(new { success = false, message = "Failed to send message" });
        }

        public async Task<IActionResult> GetMessages(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            var currentUserId = int.Parse(userIdClaim.Value);
            var chatHistory = await _messageService.GetChatHistory(currentUserId, userId);
            
            return Json(chatHistory);
        }
    }
}
