using BussinessLogic.Models;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService? _emailService;
        
        public AuthService(IUserRepository userRepository, IEmailService? emailService = null)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password)
        {
            // Check if user already exists
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(email);
            if (existingUserByEmail != null)
                return false;

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(username);
            if (existingUserByUsername != null)
                return false;

            // Hash the password
            string passwordHash = HashPassword(password);

            // Create new user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                DateCreated = DateTime.Now,
                RoleId = 3, // Assuming 3 is the regular user role
                Status = "Active"
            };

            return await _userRepository.CreateUserAsync(user);
        }

        public async Task UpdateLastLoginAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user != null)
            {
                user.LastLogin = DateTime.Now;
                await _userRepository.UpdateUserAsync(user);
            }
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return false;

            string hashedPassword = HashPassword(password);
            return user.PasswordHash == hashedPassword;
        }
        
        public async Task<bool> ExternalLoginUserAsync(string email, string username, string provider, string providerKey)
        {
            // First check if this external login already exists
            var existingUser = await _userRepository.FindUserByExternalLoginAsync(provider, providerKey);
            if (existingUser != null)
            {
                // Update last login timestamp
                existingUser.LastLogin = DateTime.Now;
                await _userRepository.UpdateUserAsync(existingUser);
                return true;
            }
            
            // Check if the user already exists by email
            var userByEmail = await _userRepository.GetUserByEmailAsync(email);
            if (userByEmail != null)
            {
                // User exists, associate external login with this user
                bool added = await _userRepository.AddExternalLoginAsync(userByEmail.UserId, provider, providerKey);
                if (added)
                {
                    userByEmail.LastLogin = DateTime.Now;
                    await _userRepository.UpdateUserAsync(userByEmail);
                }
                return added;
            }
            
            // Need to create a new user
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = Guid.NewGuid().ToString(), // Random password as they're logging in with external provider
                DateCreated = DateTime.Now,
                LastLogin = DateTime.Now,
                RoleId = 3, // Regular user role
                Status = "Active"
            };
            
            bool userCreated = await _userRepository.CreateUserAsync(newUser);
            if (!userCreated)
            {
                return false;
            }
            
            // Get the newly created user with its ID
            var createdUser = await _userRepository.GetUserByEmailAsync(email);
            if (createdUser == null)
            {
                return false;
            }
            
            // Add external login
            return await _userRepository.AddExternalLoginAsync(createdUser.UserId, provider, providerKey);
        }
        
        public async Task<User?> FindUserByExternalLoginAsync(string provider, string providerKey)
        {
            return await _userRepository.FindUserByExternalLoginAsync(provider, providerKey);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        // Password Reset Methods
        
        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return string.Empty;
            
            // Generate a unique token
            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            token = token.Replace("/", "_").Replace("+", "-").Replace("=", "");
            
            // Store the token with a 24 hour expiry
            bool created = await _userRepository.CreatePasswordResetTokenAsync(
                user.UserId, token, TimeSpan.FromHours(24));
                
            return created ? token : string.Empty;
        }
        
        public async Task<bool> ValidatePasswordResetTokenAsync(string email, string token)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return false;
                
            var resetToken = await _userRepository.GetPasswordResetTokenAsync(token);
            if (resetToken == null || resetToken.UserId != user.UserId)
                return false;
                
            return true;
        }
        
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return false;
                
            var resetToken = await _userRepository.GetPasswordResetTokenAsync(token);
            if (resetToken == null || resetToken.UserId != user.UserId)
                return false;
                
            // Mark token as used
            await _userRepository.MarkTokenAsUsedAsync(token);
            
            // Update the password
            string newPasswordHash = HashPassword(newPassword);
            return await _userRepository.UpdateUserPasswordAsync(user.UserId, newPasswordHash);
        }
        
        public async Task<bool> SendPasswordResetEmailAsync(string email, string callbackUrl)
        {
            if (_emailService == null)
                return false;
                
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return false;
                
            string subject = "Reset Your QuickMarket Password";
            
            string body = $@"
            <h2>QuickMarket Password Reset</h2>
            <p>Hello {user.Username},</p>
            <p>You requested to reset your password. Please click the link below to reset it:</p>
            <p><a href='{callbackUrl}'>Reset Password</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            <p>This link will expire in 24 hours.</p>
            <p>Thank you,<br/>QuickMarket Team</p>";
            
            return await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
