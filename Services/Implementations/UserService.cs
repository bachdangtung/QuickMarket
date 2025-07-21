using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Users;
using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<UserManagementDto> GetAllUsersAsync(string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            var pagedResult = await _userRepository.GetAllUsersAsync(page, pageSize, searchQuery ?? "");
            
            var userDtos = pagedResult.Items.Select(user => new UserListDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DateCreated = user.DateCreated,
                LastLogin = user.LastLogin,
                RoleName = user.Role.RoleName,
                Status = user.Status ?? "Active",
                ProductCount = user.Products.Count
            }).ToList();

            return new UserManagementDto
            {
                Users = userDtos,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = pagedResult.PageCount
            };
        }

        public async Task<UserListDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return null;

            return new UserListDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DateCreated = user.DateCreated,
                LastLogin = user.LastLogin,
                RoleName = user.Role.RoleName,
                Status = user.Status ?? "Active",
                ProductCount = user.Products.Count
            };
        }

        public async Task<ServiceResult> UpdateUserStatusAsync(int userId, string status)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return new ServiceResult { Success = false, Message = "User not found" };

            user.Status = status;
            await _userRepository.UpdateUserAsync(user);
            
            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> UpdateUserRoleAsync(int userId, int roleId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return new ServiceResult { Success = false, Message = "User not found" };

            user.RoleId = roleId;
            await _userRepository.UpdateUserAsync(user);
            
            return new ServiceResult { Success = true };
        }
        
        public async Task<bool> HasAdminUserAsync()
        {
            var adminUsers = await _userRepository.GetUsersByRoleNameAsync("Admin");
            return adminUsers.Any(u => u.Status != "Deleted");
        }
        
        public async Task<ServiceResult> CreateUserAsync(CreateUserDto model)
        {
            // Check if username already exists
            var existingUsername = await _userRepository.GetUserByUsernameAsync(model.Username);
            if (existingUsername != null)
            {
                return new ServiceResult { Success = false, Message = "Username already exists" };
            }
            
            // Check if email already exists
            var existingEmail = await _userRepository.GetUserByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                return new ServiceResult { Success = false, Message = "Email address already exists" };
            }
            
            // Check if trying to create Admin when one already exists
            if (model.RoleId == 1) // Assuming 1 is Admin role ID
            {
                var hasAdmin = await HasAdminUserAsync();
                if (hasAdmin)
                {
                    return new ServiceResult { Success = false, Message = "Only one administrator account is allowed" };
                }
            }
            
            // Create password hash
            string passwordHash = HashPassword(model.Password);
            
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash,
                RoleId = model.RoleId,
                DateCreated = DateTime.UtcNow,
                Status = "Active"
            };
            
            var result = await _userRepository.CreateUserAsync(user);
            
            if (result)
            {
                return new ServiceResult { Success = true };
            }
            
            return new ServiceResult { Success = false, Message = "Failed to create user" };
        }
        
        private string HashPassword(string password)
        {
            // Generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Create HMAC with SHA256
            string hashed;
            using (var hmac = new HMACSHA256(salt))
            {
                // Convert string to byte array
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                
                // Compute hash
                byte[] hashedBytes = hmac.ComputeHash(passwordBytes);
                
                // Convert to base64 string
                hashed = Convert.ToBase64String(hashedBytes);
            }
                
            // Format: {algorithm}${salt}${hash}
            return $"HMACSHA256${Convert.ToBase64String(salt)}${hashed}";
        }
    }
}
