using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Users;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IProductRepository _productRepository;

        public UserService(
            IUserRepository userRepository, 
            IMapper mapper, 
            ICloudinaryService cloudinaryService,
            IProductRepository productRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _productRepository = productRepository;
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
        
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        
        private bool VerifyPassword(string password, string storedHash)
        {
            // Phân tích chuỗi hash đã lưu
            var parts = storedHash.Split('$');
            if (parts.Length != 3)
            {
                return false;
            }

            var algorithm = parts[0];
            var saltBase64 = parts[1];
            var storedHashValue = parts[2];
            
            if (algorithm != "HMACSHA256")
            {
                return false;
            }
            
            var salt = Convert.FromBase64String(saltBase64);
            
            // Tính toán hash cho mật khẩu cung cấp
            string computedHash;
            using (var hmac = new HMACSHA256(salt))
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = hmac.ComputeHash(passwordBytes);
                computedHash = Convert.ToBase64String(hashBytes);
            }
            
            // So sánh hash đã lưu và hash vừa tính toán
            return storedHashValue == computedHash;
        }
        
        public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }
            
            // Lấy danh sách sản phẩm của người dùng (6 sản phẩm mới nhất)
            var userProducts = await _productRepository.GetProductsByUserAsync(userId);
            var recentProducts = userProducts.OrderByDescending(p => p.DatePosted).Take(6).ToList();
            
            // Lấy số lượng sản phẩm yêu thích
            var favorites = await _productRepository.GetUserFavoritesAsync(userId);
            var favoriteCount = favorites.Count;
            
            // Tạo DTO với chỉ các trường có sẵn trong model User
            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DateCreated = user.DateCreated,
                LastLogin = user.LastLogin,
                RoleName = user.Role.RoleName,
                Status = user.Status ?? "Active",
                ProductCount = userProducts.Count,
                FavoritesCount = favoriteCount,
                SoldProductsCount = userProducts.Count(p => p.Status == "Sold"),
                TotalSales = userProducts.Where(p => p.Status == "Sold").Sum(p => p.Price),
                Rating = 0,
                RecentProducts = _mapper.Map<List<ProductDto>>(recentProducts)
            };
            
            return profileDto;
        }

        public async Task<ServiceResult> UpdateProfileAsync(UpdateProfileDto model)
        {
            var user = await _userRepository.GetUserByIdAsync(model.UserId);
            if (user == null)
            {
                return ServiceResult.ErrorResult("Không tìm thấy người dùng");
            }
            
            // Kiểm tra xem username và email có trùng với người dùng khác không
            if (user.Username != model.Username)
            {
                var existingUserWithSameUsername = await _userRepository.GetUserByUsernameAsync(model.Username);
                if (existingUserWithSameUsername != null && existingUserWithSameUsername.UserId != model.UserId)
                {
                    return ServiceResult.ErrorResult("Tên đăng nhập đã tồn tại");
                }
            }
            
            if (user.Email != model.Email)
            {
                var existingUserWithSameEmail = await _userRepository.GetUserByEmailAsync(model.Email);
                if (existingUserWithSameEmail != null && existingUserWithSameEmail.UserId != model.UserId)
                {
                    return ServiceResult.ErrorResult("Email đã tồn tại");
                }
            }
            
            // Chỉ cập nhật các trường có sẵn trong model User
            user.Username = model.Username;
            user.Email = model.Email;
            
            // Lưu ý: Các trường khác như PhoneNumber, FullName, Address, Bio không có trong model User
            // nên chúng ta không cập nhật chúng
            
            // Nếu có thay đổi mật khẩu
            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                // Kiểm tra mật khẩu hiện tại
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    return ServiceResult.ErrorResult("Mật khẩu hiện tại không đúng");
                }
                
                // Đặt mật khẩu mới
                user.PasswordHash = HashPassword(model.NewPassword);
            }
            
            // Lưu thay đổi
            await _userRepository.UpdateUserAsync(user);
            
            return ServiceResult.SuccessResult("Cập nhật thông tin thành công");
        }
        
        public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.ErrorResult("Không tìm thấy người dùng");
            }
            
            // Kiểm tra mật khẩu hiện tại
            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                return ServiceResult.ErrorResult("Mật khẩu hiện tại không đúng");
            }
            
            // Đặt mật khẩu mới
            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateUserAsync(user);
            
            return ServiceResult.SuccessResult("Thay đổi mật khẩu thành công");
        }
        
        public async Task<ServiceResult> UploadProfileImageAsync(int userId, IFormFile imageFile)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.ErrorResult("Không tìm thấy người dùng");
            }
            
            try
            {
                // Trong trường hợp đơn giản hóa, chúng ta chỉ tải lên ảnh mà không thao tác với AvatarUrl
                // vì hiện tại model User không có trường này
                var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile);
                
                // Trả về URL của ảnh đã tải lên thành công
                // Ở đây chúng ta không cập nhật user vì không có trường AvatarUrl
                
                return ServiceResult.SuccessResult("Tải lên ảnh thành công", imageUrl);
            }
            catch (Exception ex)
            {
                return ServiceResult.ErrorResult($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        
        // Phương thức hỗ trợ để lấy public_id từ URL Cloudinary
        private string ExtractPublicIdFromUrl(string imageUrl)
        {
            try
            {
                // Phân tích URL để lấy public_id
                // Ví dụ: https://res.cloudinary.com/your-cloud-name/image/upload/v1624291819/users/user-123.jpg
                // Cần lấy ra phần "users/user-123"
                
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                
                // Giả sử định dạng URL là: /.../upload/v*/[public_id].*
                var uploadIndex = Array.IndexOf(segments, "upload/");
                if (uploadIndex >= 0 && uploadIndex + 2 < segments.Length)
                {
                    // Bỏ qua phần "v*/" trong URL
                    var pathAfterUpload = string.Join("", segments.Skip(uploadIndex + 2));
                    
                    // Loại bỏ phần mở rộng file (.jpg, .png, ...)
                    var extension = System.IO.Path.GetExtension(pathAfterUpload);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        return pathAfterUpload.Substring(0, pathAfterUpload.Length - extension.Length);
                    }
                    
                    return pathAfterUpload;
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
