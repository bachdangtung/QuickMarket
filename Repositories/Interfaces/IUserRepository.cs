using BussinessLogic.Models;

namespace Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<bool> AddExternalLoginAsync(int userId, string provider, string providerKey);
        Task<User?> FindUserByExternalLoginAsync(string provider, string providerKey);
    }
}
