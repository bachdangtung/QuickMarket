using BussinessLogic.DTOs.Messages;
using BussinessLogic.Models;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<bool> SendMessage(int fromUserId, int toUserId, string messageText)
        {
            try
            {
                var message = new Message
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    MessageText = messageText,
                    SentTime = DateTime.Now
                };

                await _messageRepository.CreateMessage(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ChatHistoryDto> GetChatHistory(int currentUserId, int otherUserId)
        {
            var messages = await _messageRepository.GetMessagesBetweenUsers(currentUserId, otherUserId);
            var otherUser = await _messageRepository.GetUserById(otherUserId);

            var chatHistory = new ChatHistoryDto
            {
                OtherUserId = otherUserId,
                OtherUsername = otherUser?.Username ?? "Unknown User",
                Messages = messages.Select(m => new MessageDto
                {
                    MessId = m.MessId,
                    FromUserId = m.FromUserId,
                    FromUsername = m.FromUser.Username,
                    ToUserId = m.ToUserId,
                    ToUsername = m.ToUser.Username,
                    MessageText = m.MessageText,
                    SentTime = m.SentTime
                }).ToList()
            };

            return chatHistory;
        }

        public async Task<List<ChatHistoryDto>> GetAllChats(int userId)
        {
            var chatUserIds = await _messageRepository.GetUniqueChatterIds(userId);
            var chatHistories = new List<ChatHistoryDto>();

            foreach (var chatUserId in chatUserIds)
            {
                var chatHistory = await GetChatHistory(userId, chatUserId);
                chatHistories.Add(chatHistory);
            }

            return chatHistories;
        }

        public async Task<bool> SendMessageWithProduct(int fromUserId, int toUserId, string message, int productId)
        {
            try
            {
                // First send the regular message
                var newMessage = new Message
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    MessageText = message,
                    SentTime = DateTime.Now
                };

                await _messageRepository.CreateMessage(newMessage);
                
                // Then attach the product to the chat
                await AttachProductToChat(fromUserId, toUserId, productId);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AttachProductToChat(int currentUserId, int otherUserId, int productId)
        {
            try
            {
                // Create a new ChatProduct record
                var chatProduct = new ChatProduct
                {
                    UserId1 = currentUserId,
                    UserId2 = otherUserId,
                    ProductId = productId,
                    AddedDate = DateTime.Now
                };

                await _messageRepository.AddChatProduct(chatProduct);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
