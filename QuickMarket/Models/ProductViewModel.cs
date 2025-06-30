using BussinessLogic.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QuickMarket.Models
{
    public class ProductViewModel
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

        public List<IFormFile>? ImageFiles { get; set; }

        public List<string>? ExistingImageUrls { get; set; }

        public List<ProductReviewViewModel>? Reviews { get; set; }
    }

    public class ProductReviewViewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public int? ThreadId { get; set; }
        public List<ProductReviewViewModel>? Replies { get; set; } = new List<ProductReviewViewModel>();
    }
    
    public class ProductListViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public List<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
        public string? SearchQuery { get; set; }
        public int? CategoryFilter { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
