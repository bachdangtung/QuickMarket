using System.ComponentModel.DataAnnotations;

namespace BussinessLogic.DTOs.Users
{
    public class UpdateProfileDto
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        
        // Mật khẩu hiện tại (nếu muốn đổi mật khẩu)
        public string? CurrentPassword { get; set; }
        
        // Mật khẩu mới (nếu muốn đổi mật khẩu)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        public string? NewPassword { get; set; }
        
        // Xác nhận mật khẩu mới
        [Compare("NewPassword", ErrorMessage = "Mật khẩu không khớp")]
        public string? ConfirmPassword { get; set; }
    }
}
