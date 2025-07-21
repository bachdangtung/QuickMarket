using BussinessLogic.Models;
using BussinessLogic.DTOs;
using System.Collections.Generic;

namespace Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<bool> AddExternalLoginAsync(int userId, string provider, string providerKey);
        Task<User?> FindUserByExternalLoginAsync(string provider, string providerKey);
        Task<PagedResult<User>> GetAllUsersAsync(int page = 1, int pageSize = 10, string searchTerm = "");
        Task<IEnumerable<User>> GetUsersByRoleNameAsync(string roleName);
        
        // Password Reset Methods
        Task<bool> CreatePasswordResetTokenAsync(int userId, string token, TimeSpan expiry);
        Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
        Task<bool> MarkTokenAsUsedAsync(string token);
        Task<bool> UpdateUserPasswordAsync(int userId, string newPasswordHash);
    }
}
