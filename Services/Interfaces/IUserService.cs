using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Users;
using BussinessLogic.Models;

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
    }
}
