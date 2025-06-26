using System.ComponentModel.DataAnnotations;

namespace QuickMarket.Models
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
        
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = null!;
        
        public string? ReturnUrl { get; set; }
        
        public string? LoginProvider { get; set; }
        
        public string? ProviderKey { get; set; }
    }
}
