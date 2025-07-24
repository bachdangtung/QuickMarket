using System;
using System.Collections.Generic;
using BussinessLogic.DTOs.Products;

namespace BussinessLogic.DTOs.Users
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Đã loại bỏ các trường không có trong model User:
        // PhoneNumber, FullName, Address, Bio, AvatarUrl
        public DateTime DateCreated { get; set; }
        public DateTime? LastLogin { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        public int ProductCount { get; set; }
        public int SoldProductsCount { get; set; }
        public int FavoritesCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Rating { get; set; }
        
        // Sản phẩm gần đây của người dùng
        public List<ProductDto> RecentProducts { get; set; } = new List<ProductDto>();
    }
}
