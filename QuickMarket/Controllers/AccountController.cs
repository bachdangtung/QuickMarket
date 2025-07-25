using BussinessLogic.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace QuickMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

            var isValid = await _authService.ValidateUserAsync(loginDto.Email, loginDto.Password);
            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginDto);
            }

            var user = await _authService.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(loginDto);
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = loginDto.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Update last login timestamp
            await _authService.UpdateLastLoginAsync(loginDto.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Product");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (ModelState.IsValid)
            {
                var result = await _authService.RegisterUserAsync(registerDto.Username, registerDto.Email, registerDto.Password);
                if (result)
                {
                    // Auto login after registration
                    var loginDto = new LoginDto { Email = registerDto.Email, Password = registerDto.Password, RememberMe = false };
                    return await Login(loginDto, null);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Email or username may already be in use.");
                }
            }
            return View(registerDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Product");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return View(forgotPasswordDto);
            }
            
            // Check if user exists
            var user = await _authService.GetUserByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ViewData["SuccessMessage"] = "If your email is registered, we've sent a password reset link.";
                return View();
            }
            
            // Generate reset token
            var token = await _authService.GeneratePasswordResetTokenAsync(forgotPasswordDto.Email);
            if (string.IsNullOrEmpty(token))
            {
                // Something went wrong, but don't reveal it
                ViewData["SuccessMessage"] = "If your email is registered, we've sent a password reset link.";
                return View();
            }
            
            // Generate callback URL
            var callbackUrl = Url.Action("ResetPassword", "Account", 
                new { email = forgotPasswordDto.Email, token = token }, 
                protocol: HttpContext.Request.Scheme);
            
            // Send email
            if (callbackUrl != null)
            {
                await _authService.SendPasswordResetEmailAsync(forgotPasswordDto.Email, callbackUrl);
            }
            
            ViewData["SuccessMessage"] = "If your email is registered, we've sent a password reset link.";
            return View();
        }
        
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            
            var resetPasswordDto = new ResetPasswordDto
            {
                Email = email,
                Token = token
            };
            
            return View(resetPasswordDto);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordDto);
            }
            
            // Validate token
            var isValidToken = await _authService.ValidatePasswordResetTokenAsync(resetPasswordDto.Email, resetPasswordDto.Token);
            if (!isValidToken)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired password reset token.");
                return View(resetPasswordDto);
            }
            
            // Reset password
            var result = await _authService.ResetPasswordAsync(resetPasswordDto.Email, resetPasswordDto.Token, resetPasswordDto.Password);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Error resetting password.");
                return View(resetPasswordDto);
            }
            
            return RedirectToAction("ResetPasswordConfirmation");
        }
        
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Google Login
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties 
            { 
                RedirectUri = redirectUrl,
                Items = { ["scheme"] = GoogleDefaults.AuthenticationScheme }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });

            // Get the claims
            var emailClaim = result.Principal.FindFirst(ClaimTypes.Email);
            var nameClaim = result.Principal.FindFirst(ClaimTypes.Name);
            var providerKey = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (emailClaim == null || nameClaim == null || string.IsNullOrEmpty(providerKey))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
            }

            var user = await _authService.FindUserByExternalLoginAsync("Google", providerKey);
            if (user == null)
            {
                // We need to create or associate a new user
                var registrationResult = await _authService.ExternalLoginUserAsync(
                    emailClaim.Value,
                    nameClaim.Value,
                    "Google",
                    providerKey);

                if (!registrationResult)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
                }

                user = await _authService.FindUserByExternalLoginAsync("Google", providerKey);
                if (user == null)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
                }
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Update last login timestamp
            await _authService.UpdateLastLoginAsync(user.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Product");
        }
    }
}
