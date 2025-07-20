using System.ComponentModel.DataAnnotations;

namespace BussinessLogic.DTOs.Users
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;
    }
}
