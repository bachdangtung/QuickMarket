using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessLogic.DTOs.Products
{
    public class ProductCreateUpdateDto
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = null!;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Display(Name = "Ngày đăng")]
        public DateTime DatePosted { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Danh mục sản phẩm là bắt buộc")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Danh mục")]
        public string? CategoryName { get; set; }

        [Display(Name = "Người bán")]
        public string? SellerName { get; set; }

        public int? UserId { get; set; }

        // ImageFiles will be handled in the controller
        // This allows us to keep the DTO independent of ASP.NET Core

        public List<string>? ExistingImageUrls { get; set; }
        public List<ProductReviewDto>? Reviews { get; set; }
    }
}
