using System.Security.Claims;

namespace QuickMarket.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
        {
            return principal.FindFirst(claimType)?.Value;
        }
        
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim) : 0;
        }
    }
}
