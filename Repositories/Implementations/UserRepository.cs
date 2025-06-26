using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly QuickMarketContext _context;

        public UserRepository(QuickMarketContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        
        public async Task<bool> AddExternalLoginAsync(int userId, string provider, string providerKey)
        {
            var externalLogin = new ExternalLogin
            {
                UserId = userId,
                Provider = provider,
                ProviderKey = providerKey,
                DateCreated = DateTime.Now
            };
            
            _context.ExternalLogins.Add(externalLogin);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        
        public async Task<User?> FindUserByExternalLoginAsync(string provider, string providerKey)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => _context.ExternalLogins.Any(el => 
                    el.UserId == u.UserId && 
                    el.Provider == provider && 
                    el.ProviderKey == providerKey))
                .FirstOrDefaultAsync();
        }
    }
}
