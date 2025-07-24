using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Users;
using BussinessLogic.Models;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IUserService
    {
        Task<UserManagementDto> GetAllUsersAsync(string? searchQuery = null, int page = 1, int pageSize = 10);
        Task<UserListDto?> GetUserByIdAsync(int id);
        Task<ServiceResult> UpdateUserStatusAsync(int userId, string status);
        Task<ServiceResult> UpdateUserRoleAsync(int userId, int roleId);
        Task<ServiceResult> CreateUserAsync(CreateUserDto model);
        Task<bool> HasAdminUserAsync();
        
        // User Profile methods
        Task<UserProfileDto?> GetUserProfileAsync(int userId);
        Task<ServiceResult> UpdateProfileAsync(UpdateProfileDto model);
        Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<ServiceResult> UploadProfileImageAsync(int userId, IFormFile imageFile);
    }
}
