using BussinessLogic.Models;

namespace Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> ValidateUserAsync(string email, string password);
        Task<bool> RegisterUserAsync(string username, string email, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task UpdateLastLoginAsync(string email);
        Task<bool> ExternalLoginUserAsync(string email, string username, string provider, string providerKey);
        Task<User?> FindUserByExternalLoginAsync(string provider, string providerKey);
        
        // Password Reset Methods
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ValidatePasswordResetTokenAsync(string email, string token);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string callbackUrl);
    }
}
