using BussinessLogic.DTOs.Messages;
using BussinessLogic.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IMessageService
    {
        Task<bool> SendMessage(int fromUserId, int toUserId, string message);
        Task<bool> SendMessageWithProduct(int fromUserId, int toUserId, string message, int productId);
        Task<ChatHistoryDto> GetChatHistory(int currentUserId, int otherUserId);
        Task<List<ChatHistoryDto>> GetAllChats(int userId);
        Task<bool> AttachProductToChat(int currentUserId, int otherUserId, int productId);
    }
}
