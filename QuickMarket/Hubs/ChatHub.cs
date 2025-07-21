using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace QuickMarket.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendPrivateMessage(string receiverId, string senderId, string message)
        {
            // Add message to group of seller and buyer
            string groupName = GetGroupName(senderId, receiverId);
            
            await Clients.Group(groupName).SendAsync("ReceiveMessage", senderId, message, DateTime.Now);
        }
        
        public async Task JoinChatGroup(string currentUserId, string otherUserId)
        {
            string groupName = GetGroupName(currentUserId, otherUserId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        
        public async Task LeaveChatGroup(string currentUserId, string otherUserId)
        {
            string groupName = GetGroupName(currentUserId, otherUserId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
        
        private string GetGroupName(string user1, string user2)
        {
            // Create a consistent group name regardless of who is the sender and who is the receiver
            var users = new[] { user1, user2 };
            Array.Sort(users);
            return $"chat_{users[0]}_{users[1]}";
        }
    }
}
