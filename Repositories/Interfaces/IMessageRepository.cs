using BussinessLogic.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message> CreateMessage(Message message);
        Task<List<Message>> GetMessagesBetweenUsers(int user1Id, int user2Id);
        Task<List<int>> GetUniqueChatterIds(int userId);
        Task<User> GetUserById(int userId);
        Task<ChatProduct> AddChatProduct(ChatProduct chatProduct);
    }
}
