using BussinessLogic.Models;
using BussinessLogic.DTOs;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System.Linq;

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
        
        public async Task<bool> CreatePasswordResetTokenAsync(int userId, string token, TimeSpan expiry)
        {
            var resetToken = new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiryDate = DateTime.UtcNow.Add(expiry),
                CreatedDate = DateTime.UtcNow,
                IsUsed = false
            };
            
            _context.PasswordResetTokens.Add(resetToken);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        
        public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);
        }
        
        public async Task<bool> MarkTokenAsUsedAsync(string token)
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);
                
            if (resetToken == null)
            {
                return false;
            }
            
            resetToken.IsUsed = true;
            _context.PasswordResetTokens.Update(resetToken);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        
        public async Task<bool> UpdateUserPasswordAsync(int userId, string newPasswordHash)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }
            
            user.PasswordHash = newPasswordHash;
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }
        
        public async Task<PagedResult<User>> GetAllUsersAsync(int page = 1, int pageSize = 10, string searchTerm = "")
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => 
                    u.Username.Contains(searchTerm) || 
                    u.Email.Contains(searchTerm));
            }
            
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return new PagedResult<User>
            {
                Items = users,
                TotalCount = totalItems,
                PageCount = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        
        public async Task<IEnumerable<User>> GetUsersByRoleNameAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == roleName)
                .ToListAsync();
        }
    }
}
